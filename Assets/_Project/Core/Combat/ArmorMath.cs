using System;

namespace BitRunner.Core
{
    public static class ArmorMath
    {
        private const double ArmorExponent = 1.38;
        private const double ArmorSoftCap = 92.0;

        public static float Reduction(int armor)
        {
            if (armor <= 0)
            {
                return 0f;
            }

            double scaled = Math.Pow(armor, ArmorExponent);
            return (float)(scaled / (scaled + ArmorSoftCap));
        }

        public static int ReductionPercent(int armor)
        {
            return (int)Math.Round(Reduction(armor) * 100.0);
        }

        public static int DamageAfterArmor(int rawDamage, int armor)
        {
            if (rawDamage <= 0)
            {
                return 0;
            }

            double reduced = rawDamage * (1.0 - Reduction(armor));
            return Math.Max(1, (int)Math.Floor(reduced));
        }
    }
}
