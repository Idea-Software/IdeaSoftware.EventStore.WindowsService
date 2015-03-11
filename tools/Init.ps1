param($installPath, $toolsPath, $package, $project)
Process 
    {
	$exeFileName = "IdeaSoftware.EventStore.WindowsService.exe"
        $configPath = "$installPath\EventStore\$exeFileName.config"
        $exePath = "$installPath\EventStore\$exeFileName"
	$configXml = [xml] (Get-Content $configPath)
	$node = $configXml.configuration.appSettings.add | where {$_.key -eq 'Service:EsExeLocation'}
	$node.value = $exePath
	$configXml.Save($configPath)
    } 