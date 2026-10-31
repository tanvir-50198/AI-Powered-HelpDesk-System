import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { App } from 'antd';
import { useQueryClient } from '@tanstack/react-query';
import { ApiError } from '@/api/client';
import { chatApi } from '@/api/endpoints';
import { qk } from '@/api/hooks';
import type { ChatMessage, ConversationDetail } from '@/api/types';

/**
 * Sends questions to the assistant.
 *  - the conversation is created lazily with the first question (no empty chats in the list)
 *  - the question appears immediately (optimistic), the answer is appended when it arrives
 *  - on failure the optimistic message is removed so the caller can put the text back
 */
export function useChatSession(conversationId: number | undefined) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const [pending, setPending] = useState(false);

  const patch = (id: number, update: (messages: ChatMessage[]) => ChatMessage[]) => {
    queryClient.setQueryData<ConversationDetail>(qk.conversation(id), (old) =>
      old ? { ...old, messages: update(old.messages) } : old,
    );
  };

  /** Returns true when the question was answered. */
  const send = async (question: string): Promise<boolean> => {
    setPending(true);
    let id = conversationId;

    try {
      if (id === undefined) {
        const created = await chatApi.start();
        id = created.id;
        queryClient.setQueryData<ConversationDetail>(qk.conversation(id), { ...created, messages: [] });
      }

      const optimistic: ChatMessage = {
        id: -Date.now(),
        role: 'User',
        content: question,
        isFallback: false,
        createdAt: new Date().toISOString(),
        sources: [],
        helpful: null,
      };
      patch(id, (messages) => [...messages, optimistic]);
      if (conversationId === undefined) navigate(`/assistant/${id}`, { replace: true });

      try {
        const answer = await chatApi.send(id, question);
        patch(id, (messages) => [
          ...messages,
          {
            id: answer.messageId,
            role: 'Assistant',
            content: answer.answer,
            isFallback: answer.isFallback,
            createdAt: new Date().toISOString(),
            sources: answer.sources,
            helpful: null,
          },
        ]);
        // pick up real ids / the auto-generated title
        void queryClient.invalidateQueries({ queryKey: qk.conversation(id) });
        void queryClient.invalidateQueries({ queryKey: qk.conversations });
        return true;
      } catch (error) {
        patch(id, (messages) => messages.filter((m) => m.id !== optimistic.id));
        throw error;
      }
    } catch (error) {
      const status = error instanceof ApiError ? error.status : 0;
      message.error(
        status === 429
          ? 'You’re sending questions very quickly (limit: 20 per minute). Wait a few seconds and try again.'
          : (error as Error).message,
      );
      return false;
    } finally {
      setPending(false);
    }
  };

  return { send, pending };
}
