namespace HelpDeskPro.Services.Common
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public int TotalPages
        {
            get
            {
                if (PageSize <= 0)
                {
                    return 1;
                }
                int pages = (TotalCount + PageSize - 1) / PageSize;
                if (pages < 1)
                {
                    pages = 1;
                }
                return pages;
            }
        }
    }
}
