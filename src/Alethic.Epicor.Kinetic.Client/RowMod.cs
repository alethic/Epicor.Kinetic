namespace Alethic.Epicor.Kinetic.Client;

/// <summary>
/// Values for the <c>RowMod</c> column that tells <c>Update</c> and <c>UpdateExt</c> what to do with a row.
/// </summary>
public static class RowMod
{

    /// <summary>
    /// The row is new and should be inserted.
    /// </summary>
    public const string Added = "A";

    /// <summary>
    /// The row exists and its changed columns should be saved.
    /// </summary>
    public const string Updated = "U";

    /// <summary>
    /// The row should be deleted.
    /// </summary>
    public const string Deleted = "D";

}
