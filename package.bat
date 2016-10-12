rmdir /s /q _package\
mkdir _package
copy SimGUI\bin\Release\BreadboardSim.exe _package\BreadboardSim.exe
xcopy SimGUI\res _package\res /s /i
copy LICENSE _package\LICENSE