function Get-ScriptDirectory {
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $Invocation.MyCommand.Path
}
$scriptDirectory = Get-ScriptDirectory
$exePath = "$scriptDirectory\Automation.EventStore.Topshelf.exe" 

Write-Host "Installing service"
& $exePath uninstall -servicename:EventStoreLocal
& $exePath install -servicename:EventStoreLocal -displayname:EventStoreLocal -description:EventStore -esexepath:(Get-Item -Path ".\" -Verbose).FullName


if ($LASTEXITCODE -ne 0)
{
	Write-Host Failed to install service
	Exit $LASTEXITCODE	
}
  

Write-Host "Starting service"
net start EventStoreLocal

if ($LASTEXITCODE -ne 0)
{
	Write-Host Failed to start service
	Exit $LASTEXITCODE	
}

Write-Host "Opening Web UI - login with username 'admin' and password 'changeit'"
Start-Process http://127.0.0.1:2117/

