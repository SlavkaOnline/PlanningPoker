using System;
using System.Collections.Generic;
namespace WebApi.Application
{
    public class AppSettings
    {
        public class CardType
        {
            public string Id { get; set; } = string.Empty;
            public string Caption { get; set; } = string.Empty;
            public string[] Cards { get; set; } = Array.Empty<string>();
        }

        public IEnumerable<CardType> CardTypes { get; set; }
    }
}