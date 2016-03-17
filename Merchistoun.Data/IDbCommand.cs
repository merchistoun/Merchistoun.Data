namespace Merchistoun.Data
{
	public interface IDbCommand<T>
	{
		IDbConnector<T> StoredProcedure(string storedProcedure);

		IDbConnector<T> QueryText(string queryText);
	}
}
