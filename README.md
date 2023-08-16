# Sntndr.Api

RESTful API to retrieve the details of the first n "best stories" from the Hacker News API

Prerequisite:
-git
-.net sdk 7

To run the api folow folowing steps:
- create a folder on you computer and make it the current folder  
for ex:
```
md \HackersNewsApi
cd \HackersNewsApi
```
- clone the repository
```
git clone https://github.com/sagarok/sntndr.git
```
- execute
```
dotnet run --project Sntndr.Api -lp https
```

- For testing purposes use Swagger UI:
```
https://localhost:7008/swagger/index.html
```

I am using output caching and memory caching for data, but in production it make sense to use a real distributed cache.

One my assumption is based on the task description:
```
The API should return an array of the first n "best stories" as returned by the Hacker News API, sorted by their score in a descending order,
```
I assume that it was not required to download all stories info to verify their score.
So I did a it was asked: got first N ids from the Hacker News API, then requested their infos and sort them accordingly. Otherwise the implementation would be a little different.
