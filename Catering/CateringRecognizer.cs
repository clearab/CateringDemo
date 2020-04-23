﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Catering
{
    public class CateringRecognizer
    {
        string[] invalidEntres = new string[] {
            "worm",
            "dirt",
            "rock",
            "mud",
            "earth",
            "virus"
        };

        string[] invalidDrinks = new string[] {
            "shake",
            "mud",
            "rain",
            "distilled"
        };


        public async Task<bool> ValidateEntre(string entre)
        {
            var value = entre.ToLowerInvariant();

            if (invalidEntres.Any(e => value.Contains(e)))
            {
                return false;
            }

            return true;
        }

        public async Task<bool> ValidateDrink(string drink)
        {
            var value = drink.ToLowerInvariant();

            if (invalidDrinks.Any(e => value.Contains(e)))
            {
                return false;
            }

            return true;
        }
    }
}