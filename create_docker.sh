touch measurements.txt
docker run --rm -ti --mount type=bind,src=$(pwd)/measurements.txt,dst=/measurements.txt 1brc
