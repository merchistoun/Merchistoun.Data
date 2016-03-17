using System.Transactions;

namespace Merchistoun.Data
{
	public enum OptionType
	{
		Required,
		RequiresNew,
		Suppress
	}


	static class OptionTypeExtensions
	{
		public static TransactionScopeOption Convert(this OptionType optionType)
		{
			switch (optionType)
			{
				case OptionType.RequiresNew: return TransactionScopeOption.RequiresNew;
				case OptionType.Suppress: return TransactionScopeOption.Suppress;
				default: return TransactionScopeOption.Required;
			}
		}

	}
}
