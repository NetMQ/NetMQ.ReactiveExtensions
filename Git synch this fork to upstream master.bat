rem If we are are on a fork, synchronize this fork to the master.
rem See https://help.github.com/articles/syncing-a-fork/

rem If this has already been done, then this next line will have no effect.
git remote add upstream https://github.com/NetMQ/NetMQ.ReactiveExtensions
git fetch upstream
git checkout master
git merge upstream/master
pause

