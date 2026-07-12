namespace ProTasker.Pagination
{
    public class PaginationQuery
    {
        private int _pageNumber = 1;
        public int PageNumber 
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > 50 ? 50 : (value < 1 ? 1 : value);
        }
    }
}
