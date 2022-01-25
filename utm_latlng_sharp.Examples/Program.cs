using utm_latlng_sharp;

UTMLatLng utm = new();
//  Alternatively, (w/ support for other ellipsoids)
/*  UTMLatLng utm = new(EllipsoidType.WGS84);   */

Console.WriteLine(utm.ConvertLatLngToUtm(0d, 0d));
Console.WriteLine(utm.ConvertLatLngToUtm(0d, 0d, 10));

Console.WriteLine(utm.ConvertUtmToLatLng(166021.4431793304d, 0d, 31, 'N'));
Console.WriteLine(utm.ConvertUtmToLatLng(166021.4431793304d, 0d, 31, 'N', 10));

Console.WriteLine(utm.ConvertUtmToLatLngWithHemisphere(166021.4431793304d, 0d, 31, 'N'));
Console.WriteLine(utm.ConvertUtmToLatLngWithHemisphere(166021.4431793304d, 0d, 31, 'N', 10));
