FROM eclipse-temurin:21

ADD .mvn .mvn
ADD data data
ADD etc etc
ADD src src
ADD mvnw .
ADD pom.xml .
ADD *.sh .

RUN ./mvnw clean verify -Dlicense.skipCheckLicense -Dlicense.skipDownloadLicenses -Dlicense.skipAddThirdParty -Dlicense.skip

ENTRYPOINT bash

CMD ./create_measurements.sh 1000000000
