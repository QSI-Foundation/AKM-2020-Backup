@echo off

cd AKMProxy.1
start "AKMProxy" cmd /c AKMWorkerService

cd ..\AKMClient.5
start "AKMClient.5" cmd /c AkmAutomatedTestClient 

cd ..\AKMClient.6
start "AKMClient.6" cmd /c AkmAutomatedTestClient

cd ..\AKMClient.7
start "AKMClient.7" cmd /c AkmAutomatedTestClient
cd ..

cmdow.exe "AKMProxy"    /mov 0   0 /siz 730 430
cmdow.exe "AKMClient.6" /mov 0   430 /siz 730 430

cmdow.exe "AKMClient.5" /mov 720 0 /siz 730 430
cmdow.exe "AKMClient.7" /mov 720 430 /siz 730 430






