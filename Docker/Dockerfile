# Build context = thư mục gốc của repo (xem docker-compose.yml)

# ---------- Giai đoạn 1: build ra file .jar ----------
FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /build

# Tải thư viện trước để Docker cache lại (sửa code không phải tải lại)
COPY Backend/pom.xml .
RUN mvn -B -q dependency:go-offline

COPY Backend/src ./src
RUN mvn -B -q clean package -DskipTests

# ---------- Giai đoạn 2: chạy ----------
FROM eclipse-temurin:21-jre
WORKDIR /app

RUN useradd --system --no-create-home spring
USER spring

COPY --from=build /build/target/*.jar app.jar

EXPOSE 8080
ENV JAVA_OPTS=""
ENTRYPOINT ["sh", "-c", "exec java $JAVA_OPTS -jar app.jar"]
