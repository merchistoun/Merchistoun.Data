using System.Data;

namespace Merchistoun.Data
{
	public interface IMapper<out T>
	{
		T Map(IDataRecord record);
	}
}
