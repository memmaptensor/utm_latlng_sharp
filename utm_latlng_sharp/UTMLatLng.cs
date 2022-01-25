namespace utm_latlng_sharp;

public class UTMLatLng
{
    public UTMLatLng() => SetEllipsoid(EllipsoidTypeInstance);

    public UTMLatLng(EllipsoidType ellipsoidIn)
    {
        EllipsoidTypeInstance = ellipsoidIn;
        SetEllipsoid(EllipsoidTypeInstance);
    }

    public EllipsoidType EllipsoidTypeInstance { get; } = EllipsoidType.WGS84;
    public int A { get; private set; }
    public double EccSquared { get; private set; }

    public (double Easting, double Northing, int ZoneNumber, char ZoneLetter) ConvertLatLngToUtm(double latitude,
        double longitude, int precision)
    {
        (double easting, double northing, int zoneNumber, char zoneLetter) = ConvertLatLngToUtm(latitude, longitude);
        return (PrecisionRound(easting, precision), PrecisionRound(northing, precision), zoneNumber, zoneLetter);
    }

    public (double Easting, double Northing, int ZoneNumber, char ZoneLetter) ConvertLatLngToUtm(double latitude,
        double longitude)
    {
        double longTemp = longitude;
        double latRad = ToRadians(latitude);
        double longRad = ToRadians(longTemp);
        int zoneNumber;

        if (longTemp >= 8 && longTemp <= 13 && latitude > 54.5 && latitude < 58)
        {
            zoneNumber = 32;
        }
        else if (latitude >= 56.0 && latitude < 64.0 && longTemp >= 3.0 && longTemp < 12.0)
        {
            zoneNumber = 32;
        }
        else
        {
            zoneNumber = (int)((longTemp + 180) / 6) + 1;

            if (latitude >= 72.0 && latitude < 84.0)
            {
                if (longTemp >= 0.0 && longTemp < 9.0)
                {
                    zoneNumber = 31;
                }
                else if (longTemp >= 9.0 && longTemp < 21.0)
                {
                    zoneNumber = 33;
                }
                else if (longTemp >= 21.0 && longTemp < 33.0)
                {
                    zoneNumber = 35;
                }
                else if (longTemp >= 33.0 && longTemp < 42.0)
                {
                    zoneNumber = 37;
                }
            }
        }

        int longOrigin = (zoneNumber - 1) * 6 - 180 + 3; //+3 puts origin in middle of zone
        double longOriginRad = ToRadians(longOrigin);

        char utmZone = GetUtmLetterDesignator(latitude);

        double eccPrimeSquared = EccSquared / (1 - EccSquared);

        double n = A / Math.Sqrt(1 - EccSquared * Math.Sin(latRad) * Math.Sin(latRad));
        double t = Math.Tan(latRad) * Math.Tan(latRad);
        double c = eccPrimeSquared * Math.Cos(latRad) * Math.Cos(latRad);
        double a = Math.Cos(latRad) * (longRad - longOriginRad);

        double m = A *
                   ((1 - EccSquared / 4 - 3 * EccSquared * EccSquared / 64 -
                     5 * EccSquared * EccSquared * EccSquared / 256) * latRad
                    - (3 * EccSquared / 8 + 3 * EccSquared * EccSquared / 32 +
                       45 * EccSquared * EccSquared * EccSquared / 1024) * Math.Sin(2 * latRad)
                    + (15 * EccSquared * EccSquared / 256 +
                       45 * EccSquared * EccSquared * EccSquared / 1024) * Math.Sin(4 * latRad)
                    - 35 * EccSquared * EccSquared * EccSquared / 3072 * Math.Sin(6 * latRad));

        double utmEasting = 0.9996 * n * (a + (1 - t + c) * a * a * a / 6
                                            + (5 - 18 * t + t * t + 72 * c - 58 * eccPrimeSquared) * a * a * a * a * a /
                                            120)
                            + 500000.0;

        double utmNorthing = 0.9996 * (m + n * Math.Tan(latRad) *
            (a * a / 2 + (5 - t + 9 * c + 4 * c * c) * a * a * a * a / 24
                       + (61 - 58 * t + t * t + 600 * c - 330 * eccPrimeSquared) * a * a * a * a * a * a / 720));

        if (latitude < 0)
        {
            utmNorthing += 10000000.0;
        }

        return (utmEasting, utmNorthing, zoneNumber, utmZone);
    }

    public (double Lat, double Long) ConvertUtmToLatLng(double utmEasting, double utmNorthing, int utmZoneNumber,
        char utmZoneLetter)
    {
        (double lat, double @long, _) =
            ConvertUtmToLatLngWithHemisphere(utmEasting, utmNorthing, utmZoneNumber, utmZoneLetter);
        return (lat, @long);
    }

    public (double Lat, double Long) ConvertUtmToLatLng(double utmEasting, double utmNorthing, int utmZoneNumber,
        char utmZoneLetter, int precision)
    {
        (double lat, double @long) = ConvertUtmToLatLng(utmEasting, utmNorthing, utmZoneNumber, utmZoneLetter);
        return (PrecisionRound(lat, precision), PrecisionRound(@long, precision));
    }

    public (double Lat, double Long, bool IsNorthernHemisphere) ConvertUtmToLatLngWithHemisphere(double utmEasting,
        double utmNorthing, int utmZoneNumber,
        char utmZoneLetter, int precision)
    {
        (double lat, double @long, bool isNorthernHemisphere) =
            ConvertUtmToLatLngWithHemisphere(utmEasting, utmNorthing, utmZoneNumber, utmZoneLetter);
        return (PrecisionRound(lat, precision), PrecisionRound(@long, precision), isNorthernHemisphere);
    }

    public (double Lat, double Long, bool IsNorthernHemisphere) ConvertUtmToLatLngWithHemisphere(double utmEasting,
        double utmNorthing, int utmZoneNumber,
        char utmZoneLetter)
    {
        double e1 = (1 - Math.Sqrt(1 - EccSquared)) / (1 + Math.Sqrt(1 - EccSquared));
        double x = utmEasting - 500000.0; //remove 500,000 meter offset for longitude
        double y = utmNorthing;
        char zoneLetter = utmZoneLetter;
        bool isNorthernHemisphere;

        if (new[] { 'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' }.Any(letter =>
                letter == zoneLetter))
        {
            isNorthernHemisphere = true;
        }
        else
        {
            isNorthernHemisphere = false;
            y -= 10000000.0;
        }

        int longOrigin = (utmZoneNumber - 1) * 6 - 180 + 3;

        double eccPrimeSquared = EccSquared / (1 - EccSquared);

        double M = y / 0.9996;
        double mu = M / (A * (1 - EccSquared / 4 - 3 * EccSquared * EccSquared / 64 -
                              5 * EccSquared * EccSquared * EccSquared / 256));

        double phi1Rad = mu + (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * mu)
                            + (21 * e1 * e1 / 16 - 55 * e1 * e1 * e1 * e1 / 32) * Math.Sin(4 * mu)
                            + 151 * e1 * e1 * e1 / 96 * Math.Sin(6 * mu);
        //  double phi1 = ToDegrees(phi1Rad);

        double n1 = A / Math.Sqrt(1 - EccSquared * Math.Sin(phi1Rad) * Math.Sin(phi1Rad));
        double t1 = Math.Tan(phi1Rad) * Math.Tan(phi1Rad);
        double c1 = eccPrimeSquared * Math.Cos(phi1Rad) * Math.Cos(phi1Rad);
        double r1 = A * (1 - EccSquared) /
                    Math.Pow(1 - EccSquared * Math.Sin(phi1Rad) * Math.Sin(phi1Rad), 1.5);
        double d = x / (n1 * 0.9996);

        double lat = phi1Rad - n1 * Math.Tan(phi1Rad) / r1 *
            (d * d / 2 - (5 + 3 * t1 + 10 * c1 - 4 * c1 * c1 - 9 * eccPrimeSquared) * d * d * d * d / 24
             + (61 + 90 * t1 + 298 * c1 + 45 * t1 * t1 - 252 * eccPrimeSquared - 3 * c1 * c1) * d * d * d * d * d * d /
             720);
        lat = ToDegrees(lat);

        double @long = (d - (1 + 2 * t1 + c1) * d * d * d / 6 +
                        (5 - 2 * c1 + 28 * t1 - 3 * c1 * c1 + 8 * eccPrimeSquared + 24 * t1 * t1)
                        * d * d * d * d * d / 120) / Math.Cos(phi1Rad);
        @long = longOrigin + ToDegrees(@long);

        return (lat, @long, isNorthernHemisphere);
    }

    private static char GetUtmLetterDesignator(double latitude) =>
        latitude switch
        {
            < 84 and >= 72 => 'X',
            <= 72 and >= 64 => 'W',
            <= 64 and >= 56 => 'V',
            <= 56 and >= 48 => 'U',
            <= 48 and >= 40 => 'T',
            <= 40 and >= 32 => 'S',
            <= 32 and >= 24 => 'R',
            <= 24 and >= 16 => 'Q',
            <= 16 and >= 8 => 'P',
            <= 8 and >= 0 => 'N',
            <= 0 and >= -8 => 'M',
            <= -8 and >= -16 => 'L',
            <= -16 and >= -24 => 'K',
            <= -24 and >= -32 => 'J',
            <= -32 and >= -40 => 'H',
            <= -40 and >= -48 => 'G',
            <= -48 and >= -56 => 'F',
            <= -56 and >= -64 => 'E',
            <= -64 and >= -72 => 'D',
            <= -72 and >= -80 => 'C',
            _ => 'Z'
        };

    private void SetEllipsoid(EllipsoidType type) =>
        (A, EccSquared) = type switch
        {
            EllipsoidType.Airy => (6377563, 0.00667054),
            EllipsoidType.AustralianNational => (6378160, 0.006694542),
            EllipsoidType.Bessel1841 => (6377397, 0.006674372),
            EllipsoidType.Bessel1841Nambia => (6377484, 0.006674372),
            EllipsoidType.Clarke1866 => (6378206, 0.006768658),
            EllipsoidType.Clarke1880 => (6378249, 0.006803511),
            EllipsoidType.Everest => (6377276, 0.006637847),
            EllipsoidType.Fischer1960Mercury => (6378166, 0.006693422),
            EllipsoidType.Fischer1968 => (6378150, 0.006693422),
            EllipsoidType.GRS1967 => (6378160, 0.006694605),
            EllipsoidType.GRS1980 => (6378137, 0.00669438),
            EllipsoidType.Helmert1906 => (6378200, 0.006693422),
            EllipsoidType.Hough => (6378270, 0.00672267),
            EllipsoidType.International => (6378388, 0.00672267),
            EllipsoidType.Krassovsky => (6378245, 0.006693422),
            EllipsoidType.ModifiedAiry => (6377340, 0.00667054),
            EllipsoidType.ModifiedEverest => (6377304, 0.006637847),
            EllipsoidType.ModifiedFischer1960 => (6378155, 0.006693422),
            EllipsoidType.SouthAmerican1969 => (6378160, 0.006694542),
            EllipsoidType.WGS60 => (6378165, 0.006693422),
            EllipsoidType.WGS66 => (6378145, 0.006694542),
            EllipsoidType.WGS72 => (6378135, 0.006694318),
            EllipsoidType.ED50 => (6378388, 0.00672267), // International Ellipsoid
            // Max deviation from WGS 84 is 40 cm/km see http://ocq.dk/euref89 (in danish)
            // ETRS89 same as EUREF89 
            EllipsoidType.WGS84 or EllipsoidType.EUREF89 or EllipsoidType.ETRS89 => (6378137, 0.00669438),
            _ => throw new ArgumentException($"No ecclipsoid data associated with unknown datum: {type}")
        };

    private static double ToDegrees(double rad) => rad / Math.PI * 180;

    private static double ToRadians(double deg) => deg * Math.PI / 180;

    private static double PrecisionRound(double number, int precision)
    {
        double factor = Math.Pow(10, precision);
        return Math.Round(number * factor) / factor;
    }
}
