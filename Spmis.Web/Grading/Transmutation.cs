namespace Spmis.Web.Grading;

public sealed record TransmutationRow(decimal MinRaw, decimal ReportedGrade, bool IsPassing);

// Raw grade (0-100) to reported grade (1.00-5.00): the row with the highest MinRaw
// that is not above the raw grade. The caller rounds the raw grade BEFORE the lookup.
public sealed class TransmutationTable
{
    private readonly IReadOnlyList<TransmutationRow> _rows;

    public TransmutationTable(IEnumerable<TransmutationRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var list = rows.OrderByDescending(r => r.MinRaw).ToList();

        if (list.Count == 0)
            throw new ArgumentException("A transmutation table needs at least one row.");
        if (list.Select(r => r.MinRaw).Distinct().Count() != list.Count)
            throw new ArgumentException("Two rows have the same lower bound.");
        if (list[^1].MinRaw != 0m)
            throw new ArgumentException("The lowest row must start at 0 so every raw grade has a result.");

        _rows = list;
    }

    public TransmutationRow Lookup(decimal rawGrade)
    {
        if (rawGrade < 0m || rawGrade > 100m)
            throw new ArgumentOutOfRangeException(nameof(rawGrade), rawGrade,
                "A raw grade must be between 0 and 100.");

        return _rows.First(r => r.MinRaw <= rawGrade);
    }
}
