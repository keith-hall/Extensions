$VsTestConsole = $Env:VsTestConsole
if (!$VsTestConsole) {
    $VsTestConsole = "VsTest.Console.exe"
}
$Configuration = $Env:Configuration
if (!$Configuration) {
    $Configuration = "Debug"
}

Start-Process -FilePath $VsTestConsole -ArgumentList ".\bin\$Configuration\Tests.dll" -Wait | Write-Output