using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace WebApi.Application
{
	public class CardsTypeProvider
	{
		private readonly Dictionary<string, AppSettings.CardType> _cardsTypes;
		public IEnumerable<AppSettings.CardType> CardsTypes => _cardsTypes.Values;
		public CardsTypeProvider(IOptions<AppSettings> appSettings)
		{
            _cardsTypes =
                appSettings
                    .Value
                    .CardTypes
				.GroupBy(t => t.Id)
				.ToDictionary(c => c.Key, c => c.First());
		}

		public string[] GetCardsByTypeId(string id) =>
			_cardsTypes.TryGetValue(id, out var cards) ? cards.Cards : Array.Empty<string>();
	}
}