# Automatic Scheduler
Program for repeating scheduled tasks. Tasks are based depending on passed settings and URLs.

## Domain support
- www.ceneo.pl
- www.cinema-city.pl

## Task execution
### Host: www.ceneo.pl
Periodically checks price on given URL and sends e-mail, if given requirements are met.

### Host: www.cinema-city.pl
Periodically checks, if there are available free spots and notifies the user.

## Running the program
Program was written in .Net and you need dotnet runtime to run this project. Project name with program entry is called `AutomaticScheduler.Console` and you should run executable `AutomaticScheduler.Console.dll`.

## Docker support
Project contains dockerfile file for fast program containerization. You need to pass arguments in order for this program to work properly and receive notifications. Check dockerfile and execute program with `-h` flag to learn more about all available options.

Image building example:
```docker
docker build -t automatic_scheduler:latest --build-arg log_path="/logs/automatic_scheduler.txt" --build-arg sender_mail="sender@email" --build-arg sender_pass="sender_email_password" --build-arg target_mail="target.receiver@email" --build-arg url="https://www.ceneo.pl/134956979" .
```
