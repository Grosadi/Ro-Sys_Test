Ro-Sys Teszt Feladat

1. Első lépésben kinyerem az adatokat az adatbázisból egy előre megírt SQL query-vel. 
Az így kapott adat 512x512 sorból áll, amelyek tartalmazzák a raster egyes pontjainak értékék, koordinátáját, sor és oszlop számát.
2. Ezután az így kapott cellákból érték mátrixot generálok, majd kiszámolom a szomszédos cellák értékeinek átlagát.
3. Ezután K-Means algoritmust használva, 3 diszkrét csoportba sorolom a cellákat és K-Means által visszaadott címkék alapján beállítom az egyes cellák cluster értékét.
4. A következő lépés a polygonok generálása, amelyet az OpenCv FindContours metódus segítségével végzek el.
5. Az így kapott változó méretű polygonok-at megpróbálom egyesíteni, majd kiszelektálom terület alapján.
6. A megmaradt polygonokhoz hozzá rendelem a hozzátartozó cellákat, majd multipolygonokat képzek belőlük.
7. Ezt az eredményt fájlba írom.
