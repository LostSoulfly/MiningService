# MiningService
TopShelf service and userland idle monitor for easy crypto mining

Comprised of two separate programs.

This is an idle miner software; it should detect whether your computer is idle or locked and mine accordingly based on your settings. Most features are implemented and (should be) working with the current release.

### IdleService

Runs as a Windows Service, and starts user-defined (crypto ideally, but anything, really) programs in the SYSTEM user's context.
This allows us easy access to large pages (necessary for fast CPU mining with Monero), as well as easily hiding the mining software from display.
If no logged in user is found, it will launch the user-defined software for the appropriate condition.

### IdleMon

Runs in the current logged in user's Desktop environment, automatically started by the IdleService using CreateProcessAsUser.
IdleMon starts a NamedPipe server which IdleService connects to and, using that pipe, tells the service whether the user is idle or if the user chooses, through a task tray icon, to pause mining.


## Current todo list
- [X] Most things work!
- [X] Implement configuration loading from a JSON file
- [X] Load user-defined programs for different conditions
- [X] More complete NamedPipe (IdleService -> IdleMon) communication and features
- [X] Add a stealth mode, which hides the task tray options (you must recompile it with the stealthMode option set to true in IdleMon)
- [X] Add configuration options like debug output, log file, and network connectivity checking (connectivity checking is implemented, but not used or tested)
- [X] Check if a program is running fullscreen as the current logged on user, and stop mining if configured to in the settings
- [X] Monitor CPU usage and stop non-Idle mining if over a set threshold
- [ ] GUI to easily modify configuration JSON
- [ ] Average the CPU usage over a period of time longer than 1s, and stop programs if usage is too high while not idle
- [ ] Monitor GPU temperature and stop programs if over a threshold
- [ ] Monitor CPU temperature and stop programs if over a threshold

### Credits
https://github.com/acdvorak/named-pipe-wrapper (I've modified it to keep CPU usage lower, check the PRs for example)
https://github.com/murrayju/CreateProcessAsUser (I was not able to get command args working on Win10)
https://stackoverflow.com/questions/3743956/is-there-a-way-to-check-to-see-if-another-program-is-running-full-screen
Newtonsoft.Json
TopShelf
And probably one or two I'm not thinking of.
