FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY fletesProyect.csproj ./
RUN dotnet restore fletesProyect.csproj

COPY . ./
RUN dotnet publish fletesProyect.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

CMD ["sh", "-c", "dotnet fletesProyect.dll --urls http://0.0.0.0:${PORT:-8080}"]
