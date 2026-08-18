FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN apt-get update && apt-get install -y --no-install-recommends \
  clang \
  zlib1g-dev \
  && rm -rf /var/lib/apt/lists/*

COPY ["ApiGateway.csproj", "./"]
RUN dotnet restore "ApiGateway.csproj" -r linux-x64

COPY . .
RUN dotnet publish "ApiGateway.csproj" -c Release -r linux-x64 -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["./ApiGateway"]