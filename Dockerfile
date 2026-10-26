FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/StudySpace.Core/StudySpace.Core.csproj", "src/StudySpace.Core/"]
COPY ["src/StudySpace.Data/StudySpace.Data.csproj", "src/StudySpace.Data/"]
COPY ["src/StudySpace.Web/StudySpace.Web.csproj", "src/StudySpace.Web/"]
RUN dotnet restore src/StudySpace.Web/StudySpace.Web.csproj

COPY . .
RUN dotnet publish src/StudySpace.Web/StudySpace.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN mkdir -p /app/data /app/wwwroot/uploads
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "StudySpace.Web.dll"]

