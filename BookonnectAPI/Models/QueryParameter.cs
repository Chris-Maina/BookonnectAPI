namespace BookonnectAPI.Models;

public class QueryParameter
{
	const int _maxSize = 100;
	private int _size = 50;
	private string _sortOrder = "asc";

	public int Size
	{
		get => _size;
		set
		{
			_size = Math.Min(_maxSize, value);
		}
	}

	public string SortOrder
	{
		get => _sortOrder;
		set
		{
			if (value == "asc" || value == "desc")
			{
				_sortOrder = value;
			}
		}
	}

	public int Page { get; set; } = 1;
    public string? SortBy { get; set; }
}

