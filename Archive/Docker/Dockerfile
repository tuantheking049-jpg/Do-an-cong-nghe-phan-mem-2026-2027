FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /build
COPY Backend/pom.xml .
RUN mvn -B -q dependency:go-offline
COPY Backend/src ./src
RUN mvn -B -q clean package -DskipTests

FROM eclipse-temurin:21-jre
WORKDIR /app
RUN useradd --system --no-create-home spring
USER spring
COPY --from=build /build/target/*.jar app.jar
EXPOSE 8080
ENTRYPOINT ["java", "-jar", "app.jar"]
