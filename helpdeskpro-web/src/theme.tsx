import { createContext, useCallback, useContext, useEffect, useLayoutEffect, useMemo, useState, type ReactNode } from 'react';
import { App as AntApp, ConfigProvider, theme as antdTheme, type ThemeConfig } from 'antd';

/**
 * Design tokens
 *  - petrol  : the one brand colour (support = calm, dependable)
 *  - the sidebar is always deep petrol so navigation feels anchored in both modes
 *  - status / priority colours carry meaning, so they are never used decoratively
 */
export const PALETTE = {
  petrol: '#0e6f73',
  petrolDeep: '#0b2f33',
  petrolMist: '#e4f1f1',
  mint: '#5eead4',
  canvasLight: '#f3f6f6',
  canvasDark: '#0c1416',
  surfaceDark: '#121d20',
};

type Mode = 'light' | 'dark';
const STORAGE_KEY = 'hdp-theme';

function initialMode(): Mode {
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved === 'light' || saved === 'dark') return saved;
  } catch {
    /* storage can be blocked */
  }
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

interface ThemeContextValue {
  mode: Mode;
  toggle: () => void;
}
const ThemeContext = createContext<ThemeContextValue>({ mode: 'light', toggle: () => undefined });

// eslint-disable-next-line react-refresh/only-export-components
export const useThemeMode = () => useContext(ThemeContext);

function buildTheme(mode: Mode): ThemeConfig {
  const dark = mode === 'dark';
  return {
    algorithm: dark ? antdTheme.darkAlgorithm : antdTheme.defaultAlgorithm,
    token: {
      colorPrimary: dark ? '#2bb3b8' : PALETTE.petrol,
      colorInfo: dark ? '#4a9be8' : '#2a6fc4',
      colorLink: dark ? '#2bb3b8' : PALETTE.petrol,
      colorLinkHover: dark ? '#5fd0d4' : '#0a5558',
      colorSuccess: dark ? '#4cc38a' : '#1f8f5f',
      colorWarning: dark ? '#e8a23a' : '#c2610c',
      colorError: dark ? '#ef6a70' : '#c0262d',
      colorBgLayout: dark ? PALETTE.canvasDark : PALETTE.canvasLight,
      colorBgContainer: dark ? PALETTE.surfaceDark : '#ffffff',
      colorBgElevated: dark ? '#17252a' : '#ffffff',
      colorText: dark ? '#e6eef0' : '#14262a',
      colorTextSecondary: dark ? '#9fb3b8' : '#4a5f65',
      colorBorderSecondary: dark ? '#1f3036' : '#e2e9ea',
      borderRadius: 8,
      borderRadiusLG: 12,
      fontFamily:
        "'Figtree Variable', 'Figtree', system-ui, -apple-system, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif",
      fontSize: 14,
      controlHeight: 36,
      wireframe: false,
    },
    components: {
      Layout: {
        siderBg: PALETTE.petrolDeep,
        headerBg: dark ? PALETTE.surfaceDark : '#ffffff',
        headerHeight: 60,
        headerPadding: '0 24px',
      },
      Menu: {
        darkItemBg: PALETTE.petrolDeep,
        darkSubMenuItemBg: PALETTE.petrolDeep,
        darkItemSelectedBg: 'rgba(94, 234, 212, 0.14)',
        darkItemSelectedColor: PALETTE.mint,
        darkItemHoverBg: 'rgba(255, 255, 255, 0.07)',
        darkItemColor: 'rgba(230, 244, 244, 0.78)',
        itemBorderRadius: 8,
        itemMarginInline: 10,
        itemHeight: 42,
        iconSize: 17,
      },
      Table: {
        headerBg: dark ? '#15242a' : '#f6f9f9',
        headerColor: dark ? '#9fb3b8' : '#4a5f65',
        rowHoverBg: dark ? '#16262b' : '#f4f9f9',
        headerSplitColor: 'transparent',
      },
      Card: { headerFontSize: 15 },
      Button: { fontWeight: 500 },
      Tag: { borderRadiusSM: 6 },
    },
  };
}

/**
 * Ant Design 5 keeps its tokens in JS. styles.css (and a few inline styles) use them as
 * `var(--ant-color-*)`, so we publish the ones we need on <html>, for the active mode.
 */
const BRIDGED_TOKENS = {
  '--ant-color-primary': 'colorPrimary',
  '--ant-color-text': 'colorText',
  '--ant-color-text-secondary': 'colorTextSecondary',
  '--ant-color-text-tertiary': 'colorTextTertiary',
  '--ant-color-border': 'colorBorder',
  '--ant-color-border-secondary': 'colorBorderSecondary',
  '--ant-color-bg-container': 'colorBgContainer',
  '--ant-color-bg-layout': 'colorBgLayout',
  '--ant-color-fill-tertiary': 'colorFillTertiary',
  '--ant-color-success': 'colorSuccess',
  '--ant-color-warning': 'colorWarning',
  '--ant-color-error': 'colorError',
} as const;

function TokenBridge() {
  const { token } = antdTheme.useToken();
  useLayoutEffect(() => {
    const root = document.documentElement.style;
    for (const [cssVar, key] of Object.entries(BRIDGED_TOKENS)) {
      root.setProperty(cssVar, String(token[key as keyof typeof token]));
    }
  }, [token]);
  return null;
}

export function AppThemeProvider({ children }: { children: ReactNode }) {
  const [mode, setMode] = useState<Mode>(initialMode);

  useEffect(() => {
    document.documentElement.dataset.theme = mode;
    document.documentElement.style.colorScheme = mode;
    try {
      localStorage.setItem(STORAGE_KEY, mode);
    } catch {
      /* ignore */
    }
  }, [mode]);

  const toggle = useCallback(() => setMode((m) => (m === 'dark' ? 'light' : 'dark')), []);
  const value = useMemo(() => ({ mode, toggle }), [mode, toggle]);
  const config = useMemo(() => buildTheme(mode), [mode]);

  return (
    <ThemeContext.Provider value={value}>
      <ConfigProvider theme={config}>
        <TokenBridge />
        <AntApp>{children}</AntApp>
      </ConfigProvider>
    </ThemeContext.Provider>
  );
}
