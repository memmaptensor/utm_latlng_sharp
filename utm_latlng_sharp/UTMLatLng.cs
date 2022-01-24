namespace utm_latlng_sharp;

public class UTMLatLng
{
    public UTMLatLng() => setEllipsoid(datumName);

    public UTMLatLng(string datumNameIn)
    {
        datumName = datumNameIn;
        setEllipsoid(datumName);
    }

    /* global UTMLatLng */
    public string datumName { get; } = "WGS 84";
    public int a { get; private set; }
    public double eccSquared { get; private set; }
    public bool status { get; private set; }

    public (double Easting, double Northing, int ZoneNumber, char ZoneLetter) convertLatLngToUtm(double latitude,
        double longitude, int precision)
    {
        if (status)
        {
            throw new ArgumentException($"No ecclipsoid data associated with unknown datum: {datumName}");
        }

        double LongTemp = longitude;
        double LatRad = toRadians(latitude);
        double LongRad = toRadians(LongTemp);
        int ZoneNumber;

        if (LongTemp >= 8 && LongTemp <= 13 && latitude > 54.5 && latitude < 58)
        {
            ZoneNumber = 32;
        }
        else if (latitude >= 56.0 && latitude < 64.0 && LongTemp >= 3.0 && LongTemp < 12.0)
        {
            ZoneNumber = 32;
        }
        else
        {
            ZoneNumber = (int)((LongTemp + 180) / 6) + 1;

            if (latitude >= 72.0 && latitude < 84.0)
            {
                if (LongTemp >= 0.0 && LongTemp < 9.0)
                {
                    ZoneNumber = 31;
                }
                else if (LongTemp >= 9.0 && LongTemp < 21.0)
                {
                    ZoneNumber = 33;
                }
                else if (LongTemp >= 21.0 && LongTemp < 33.0)
                {
                    ZoneNumber = 35;
                }
                else if (LongTemp >= 33.0 && LongTemp < 42.0)
                {
                    ZoneNumber = 37;
                }
            }
        }

        int LongOrigin = (ZoneNumber - 1) * 6 - 180 + 3; //+3 puts origin in middle of zone
        double LongOriginRad = toRadians(LongOrigin);

        char UTMZone = getUtmLetterDesignator(latitude);

        double eccPrimeSquared = eccSquared / (1 - eccSquared);

        double N = a / Math.Sqrt(1 - eccSquared * Math.Sin(LatRad) * Math.Sin(LatRad));
        double T = Math.Tan(LatRad) * Math.Tan(LatRad);
        double C = eccPrimeSquared * Math.Cos(LatRad) * Math.Cos(LatRad);
        double A = Math.Cos(LatRad) * (LongRad - LongOriginRad);

        double M = a *
                   ((1 - eccSquared / 4 - 3 * eccSquared * eccSquared / 64 -
                     5 * eccSquared * eccSquared * eccSquared / 256) * LatRad
                    - (3 * eccSquared / 8 + 3 * eccSquared * eccSquared / 32 +
                       45 * eccSquared * eccSquared * eccSquared / 1024) * Math.Sin(2 * LatRad)
                    + (15 * eccSquared * eccSquared / 256 +
                       45 * eccSquared * eccSquared * eccSquared / 1024) * Math.Sin(4 * LatRad)
                    - 35 * eccSquared * eccSquared * eccSquared / 3072 * Math.Sin(6 * LatRad));

        double UTMEasting = 0.9996 * N * (A + (1 - T + C) * A * A * A / 6
                                            + (5 - 18 * T + T * T + 72 * C - 58 * eccPrimeSquared) * A * A * A * A * A /
                                            120)
                            + 500000.0;

        double UTMNorthing = 0.9996 * (M + N * Math.Tan(LatRad) *
            (A * A / 2 + (5 - T + 9 * C + 4 * C * C) * A * A * A * A / 24
                       + (61 - 58 * T + T * T + 600 * C - 330 * eccPrimeSquared) * A * A * A * A * A * A / 720));

        if (latitude < 0)
        {
            UTMNorthing += 10000000.0;
        }

        UTMNorthing = precisionRound(UTMNorthing, precision);
        UTMEasting = precisionRound(UTMEasting, precision);
        return (UTMEasting, UTMNorthing, ZoneNumber, UTMZone);
    }

    public (double Lat, double Long) convertUtmToLatLng(double UTMEasting, double UTMNorthing, int UTMZoneNumber,
        char UTMZoneLetter)
    {
        double e1 = (1 - Math.Sqrt(1 - eccSquared)) / (1 + Math.Sqrt(1 - eccSquared));
        double x = UTMEasting - 500000.0; //remove 500,000 meter offset for longitude
        double y = UTMNorthing;
        int ZoneNumber = UTMZoneNumber;
        char ZoneLetter = UTMZoneLetter;
        int NorthernHemisphere;

        if (new[] { 'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' }.Any(letter =>
                letter == ZoneLetter))
        {
            NorthernHemisphere = 1;
        }
        else
        {
            NorthernHemisphere = 0;
            y -= 10000000.0;
        }

        int LongOrigin = (ZoneNumber - 1) * 6 - 180 + 3;

        double eccPrimeSquared = eccSquared / (1 - eccSquared);

        double M = y / 0.9996;
        double mu = M / (a * (1 - eccSquared / 4 - 3 * eccSquared * eccSquared / 64 -
                              5 * eccSquared * eccSquared * eccSquared / 256));

        double phi1Rad = mu + (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * mu)
                            + (21 * e1 * e1 / 16 - 55 * e1 * e1 * e1 * e1 / 32) * Math.Sin(4 * mu)
                            + 151 * e1 * e1 * e1 / 96 * Math.Sin(6 * mu);
        double phi1 = toDegrees(phi1Rad);

        double N1 = a / Math.Sqrt(1 - eccSquared * Math.Sin(phi1Rad) * Math.Sin(phi1Rad));
        double T1 = Math.Tan(phi1Rad) * Math.Tan(phi1Rad);
        double C1 = eccPrimeSquared * Math.Cos(phi1Rad) * Math.Cos(phi1Rad);
        double R1 = a * (1 - eccSquared) /
                    Math.Pow(1 - eccSquared * Math.Sin(phi1Rad) * Math.Sin(phi1Rad), 1.5);
        double D = x / (N1 * 0.9996);

        double Lat = phi1Rad - N1 * Math.Tan(phi1Rad) / R1 *
            (D * D / 2 - (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * eccPrimeSquared) * D * D * D * D / 24
             + (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * eccPrimeSquared - 3 * C1 * C1) * D * D * D * D * D * D /
             720);
        Lat = toDegrees(Lat);

        double Long = (D - (1 + 2 * T1 + C1) * D * D * D / 6 +
                       (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * eccPrimeSquared + 24 * T1 * T1)
                       * D * D * D * D * D / 120) / Math.Cos(phi1Rad);
        Long = LongOrigin + toDegrees(Long);
        return (Lat, Long);
    }


    private char getUtmLetterDesignator(double latitude)
    {
        if (84 >= latitude && latitude >= 72)
        {
            return 'X';
        }

        if (72 > latitude && latitude >= 64)
        {
            return 'W';
        }

        if (64 > latitude && latitude >= 56)
        {
            return 'V';
        }

        if (56 > latitude && latitude >= 48)
        {
            return 'U';
        }

        if (48 > latitude && latitude >= 40)
        {
            return 'T';
        }

        if (40 > latitude && latitude >= 32)
        {
            return 'S';
        }

        if (32 > latitude && latitude >= 24)
        {
            return 'R';
        }

        if (24 > latitude && latitude >= 16)
        {
            return 'Q';
        }

        if (16 > latitude && latitude >= 8)
        {
            return 'P';
        }

        if (8 > latitude && latitude >= 0)
        {
            return 'N';
        }

        if (0 > latitude && latitude >= -8)
        {
            return 'M';
        }

        if (-8 > latitude && latitude >= -16)
        {
            return 'L';
        }

        if (-16 > latitude && latitude >= -24)
        {
            return 'K';
        }

        if (-24 > latitude && latitude >= -32)
        {
            return 'J';
        }

        if (-32 > latitude && latitude >= -40)
        {
            return 'H';
        }

        if (-40 > latitude && latitude >= -48)
        {
            return 'G';
        }

        if (-48 > latitude && latitude >= -56)
        {
            return 'F';
        }

        if (-56 > latitude && latitude >= -64)
        {
            return 'E';
        }

        if (-64 > latitude && latitude >= -72)
        {
            return 'D';
        }

        if (-72 > latitude && latitude >= -80)
        {
            return 'C';
        }

        return 'Z';
    }

    private void setEllipsoid(string name)
    {
        switch (name)
        {
            case "Airy":
                a = 6377563;
                eccSquared = 0.00667054;
                break;
            case "Australian National":
                a = 6378160;
                eccSquared = 0.006694542;
                break;
            case "Bessel 1841":
                a = 6377397;
                eccSquared = 0.006674372;
                break;
            case "Bessel 1841 Nambia":
                a = 6377484;
                eccSquared = 0.006674372;
                break;
            case "Clarke 1866":
                a = 6378206;
                eccSquared = 0.006768658;
                break;
            case "Clarke 1880":
                a = 6378249;
                eccSquared = 0.006803511;
                break;
            case "Everest":
                a = 6377276;
                eccSquared = 0.006637847;
                break;
            case "Fischer 1960 Mercury":
                a = 6378166;
                eccSquared = 0.006693422;
                break;
            case "Fischer 1968":
                a = 6378150;
                eccSquared = 0.006693422;
                break;
            case "GRS 1967":
                a = 6378160;
                eccSquared = 0.006694605;
                break;
            case "GRS 1980":
                a = 6378137;
                eccSquared = 0.00669438;
                break;
            case "Helmert 1906":
                a = 6378200;
                eccSquared = 0.006693422;
                break;
            case "Hough":
                a = 6378270;
                eccSquared = 0.00672267;
                break;
            case "International":
                a = 6378388;
                eccSquared = 0.00672267;
                break;
            case "Krassovsky":
                a = 6378245;
                eccSquared = 0.006693422;
                break;
            case "Modified Airy":
                a = 6377340;
                eccSquared = 0.00667054;
                break;
            case "Modified Everest":
                a = 6377304;
                eccSquared = 0.006637847;
                break;
            case "Modified Fischer 1960":
                a = 6378155;
                eccSquared = 0.006693422;
                break;
            case "South American 1969":
                a = 6378160;
                eccSquared = 0.006694542;
                break;
            case "WGS 60":
                a = 6378165;
                eccSquared = 0.006693422;
                break;
            case "WGS 66":
                a = 6378145;
                eccSquared = 0.006694542;
                break;
            case "WGS 72":
                a = 6378135;
                eccSquared = 0.006694318;
                break;
            case "ED50":
                a = 6378388;
                eccSquared = 0.00672267;
                break; // International Ellipsoid
            case "WGS 84":
            case "EUREF89": // Max deviation from WGS 84 is 40 cm/km see http://ocq.dk/euref89 (in danish)
            case "ETRS89": // Same as EUREF89 
                a = 6378137;
                eccSquared = 0.00669438;
                break;
            default:
                status = true;
                throw new ArgumentException($"No ecclipsoid data associated with unknown datum: {name}");
        }
    }

    private double toDegrees(double rad) => rad / Math.PI * 180;

    private double toRadians(double deg) => deg * Math.PI / 180;

    private double precisionRound(double number, int precision)
    {
        double factor = Math.Pow(10, precision);
        return Math.Round(number * factor) / factor;
    }
}
