using utm_latlng_sharp;

UTMLatLng utm = new UTMLatLng();
//  Alternatively, (w/ support for other ellipsoids)
/*  var utm = new UTMLatLang("WGS 84"); */
Console.WriteLine(utm.convertLatLngToUtm(0d, 0d, 10));
Console.WriteLine(utm.convertUtmToLatLng(166021.44d, 0d, 31, 'N'));
