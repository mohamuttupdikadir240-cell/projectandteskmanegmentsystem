namespace RealEstatePMS.ViewModels;

public class PaginationViewModel
{
    public int PageIndex { get; }
    public int TotalPages { get; }

    public PaginationViewModel(int pageIndex, int totalPages)
    {
        PageIndex = pageIndex;
        TotalPages = totalPages;
    }

    public bool HasPrevious => PageIndex > 1;
    public bool HasNext => PageIndex < TotalPages;
}
