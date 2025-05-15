# Movie Binge Application

**Movie Binge** is a .NET-based web application that enables users to register, upload, stream, and manage movies. Built using ASP.NET Core MVC, it integrates various AWS services for scalable deployment and data management.

## Features

### User Authentication
- User registration and login implemented with ASP.NET Identity.
- Credentials securely stored using AWS Systems Manager Parameter Store.
- User data stored in Microsoft SQL Server via Amazon RDS.

### Movie Management
- Upload movies (stored in Amazon S3).
- Metadata stored in DynamoDB (title, genre, director, release date, etc.).
- Only the uploader can delete or modify a movie.
- Ability to download movies.
- List movies by rating or genre using secondary indexes in DynamoDB.

### Comments and Ratings
- Users can add comments and rate movies.
- View all comments and average rating for each movie.
- Users can edit their own comments and ratings within 24 hours.

## AWS Integration
- Microsoft SQL Server hosted on Amazon RDS.
- Movie files stored in Amazon S3.
- DynamoDB used for metadata and indexing.
- Credentials managed via Parameter Store.
- Deployed using AWS Elastic Beanstalk.

## Technologies Used
- ASP.NET Core MVC
- Entity Framework Core
- SQL Server (RDS)
- Amazon S3, DynamoDB, Parameter Store
- AWS Elastic Beanstalk

## Notes
This project emphasizes .NET features including identity management, MVC architecture, and clean separation of concerns, while leveraging AWS for deployment and data storage.

