# MiningService
TopShelf service and userland idle monitor for easy crypto mining

Comprised of two separate programs.


### IdleService

Runs as a Windows Service, and starts user-defined (crypto ideally, but anything, really) programs in the SYSTEM user's context.
This allows us easy access to large pages (necessary for fast CPU mining with Monero), as well as easily hiding the mining software from display.
If no logged in user is found, it will launch the user-defined software for the appropriate condition.

### IdleMon

Runs in the current logged in user's Desktop environment, automatically started by the IdleService using CreateProcessAsUser.
IdleMon starts a NamedPipe server which IdleService connects to and, using that pipe, tells the service whether the user is idle or if the user chooses, through a task tray icon, to pause mining.


## Current todo list
- [X] Make project compilable
- [X] Implement configuration loading from file, probably JSON
- [ ] Load user-defined programs for different conditions
- [ ] More complete NamedPipe communication and features
- [ ] Average the CPU usage over a period of time longer than 1s, and stop programs if usage is too high while not idle
- [ ] Monitor GPU temperature and stop programs if over a threshold
- [ ] Monitor CPU temperature and stop programs if over a threshold
- [ ] Add a stealth mode, which hides the task tray options
- [ ] Add configuration options like debug output, log file, and network connectivity checking
- [ ] Check if a program is running fullscreen as the current logged on user, and stop programs if detected

### Credits
https://github.com/acdvorak/named-pipe-wrapper
https://github.com/murrayju/CreateProcessAsUser
https://stackoverflow.com/questions/3743956/is-there-a-way-to-check-to-see-if-another-program-is-running-full-screen
Newtonsoft.Json
TopShelf
