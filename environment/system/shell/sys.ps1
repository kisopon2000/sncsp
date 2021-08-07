param([string]$Mode='',  # �N�����[�h
      [switch]$Help)

if($script:Help){
    Write-Host "-----------------------------------------------------------"
    Write-Host " [Script]"
    Write-Host "   sys.ps1"
    Write-Host " [Parameters]"
    Write-Host "   -Mode <Launch Mode> : default=''"
    Write-Host "-----------------------------------------------------------"
    exit 1
}

# �萔�錾
Set-Variable SYS_CUR_DIR               "$(Split-Path $myInvocation.MyCommand.Path -parent)\"
Set-Variable SYS_CONFIG                "$script:SYS_CUR_DIR\..\..\config\config.xml"
Set-Variable SYS_PATH                  "$script:SYS_CUR_DIR\..\bin"
Set-Variable SYS_MODE_START            'start'
Set-Variable SYS_MODE_START_COUNTER    'start-counter'
Set-Variable SYS_MODE_START_COLLECTOR  'start-collector'
Set-Variable SYS_MODE_STOP             'stop'

function sys-start()
{
    # �N�����Ă������~
    sys-stop

    # �V�X�e���p�X�ǉ�
    $ENV:Path+=";$script:SYS_PATH"

    # ���ϐ��ǉ�
    $config_path = Convert-Path $script:SYS_CONFIG
    $ENV:KISOBE_CONFIG="$config_path"

    # Counter�N��
    powershell.exe -noprofile -ExecutionPolicy RemoteSigned $script:SYS_CUR_DIR\sys.ps1 -Mode start-counter

    # Collector�N��
    powershell.exe -noprofile -ExecutionPolicy RemoteSigned $script:SYS_CUR_DIR\sys.ps1 -Mode start-collector
}

function sys-start-counter()
{
    $xml = [XML](Get-Content $script:SYS_CONFIG) 
    $counter_num = $xml.config.counter_num
    for($i = 1; $i -le $counter_num; $i++){
        $cmd = "Counter.exe /Number $i"
        Start-Process -FilePath cmd.exe -ArgumentList "/c $cmd" -WindowStyle Hidden
    }
}

function sys-start-collector()
{
    $cmd = "Collector.exe /Stdout"
    Start-Process -FilePath cmd.exe -ArgumentList "/c $cmd"
}

function sys-stop()
{
    # Counter��~
    $counter_path = "$script:SYS_CUR_DIR\..\bin\Counter.exe"
    $counter_path = Convert-Path $counter_path
    try{
        $counters = Get-Process -Name 'Counter' -ErrorAction Stop
    }catch{
        # ���݂��Ȃ�
    }
    foreach($counter in $counters){
        if($counter.Path -eq $counter_path){
            Stop-Process -id $counter.Id
        }
    }

    # Collector��~
    $collector_path = "$script:SYS_CUR_DIR\..\bin\Collector.exe"
    $collector_path = Convert-Path $collector_path
    try{
        $collectors = Get-Process -Name 'Collector' -ErrorAction Stop
    }catch{
        # ���݂��Ȃ�
    }
    foreach($collector in $collectors){
        if($collector.Path -eq $collector_path){
            Stop-Process -id $collector.Id
        }
    }
}

# ���C���֐�
function main()
{
    switch ($script:Mode) {
    $script:SYS_MODE_START { sys-start }
    $script:SYS_MODE_START_COUNTER { sys-start-counter }
    $script:SYS_MODE_START_COLLECTOR { sys-start-collector }
    $script:SYS_MODE_STOP { sys-stop }
    default { 'Unknown' }
    }
}

# �G���g���[�|�C���g
main
