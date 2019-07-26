FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base

WORKDIR /app
COPY ./publish .

EXPOSE 80

ENTRYPOINT ["dotnet", "GameCalendarApi.Web.dll"]'''
