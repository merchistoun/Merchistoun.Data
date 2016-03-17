namespace Merchistoun.Data
{
	public interface IDbSource
	{
		IDbCommand<T> CreateCommand<T>();

		IDbCommand<string> CreateCommand();
	}
}
