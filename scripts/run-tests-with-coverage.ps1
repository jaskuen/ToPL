#!/usr/bin/env powershell
#
# Запускает тесты со сбором данных о покрытии кода тестами.
# Обрабатывает эти данные, генерируя отчёт о покрытии тестами.
#
# Установка инструментов для обработки данных о покрытии кода тестами:
#   dotnet tool install --global dotnet-coverage
#   dotnet tool install --global dotnet-reportgenerator-globaltool
#
# По завершению скрипта создаётся HTML-отчёт: tests/coverage-report/index.html

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'


#!/usr/bin/env bash
#
# Запускает тесты со сбором данных о покрытии кода тестами.
# Обрабатывает эти данные, генерируя отчёт о покрытии тестами.
#
# Установка инструментов для обработки данных о покрытии кода тестами:
#   dotnet tool install --global dotnet-coverage
#   dotnet tool install --global dotnet-reportgenerator-globaltool
#
# По завершению скрипта создаётся HTML-отчёт: tests/coverage-report/index.html

$ErrorActionPreference = 'Stop'

$ProjectDir = Split-Path -Path $PSScriptRoot -Parent

# Печатает команду, затем запускает её и проверяет код возврата.
function EchoAndCall {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Command,

        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    # Сохраняем в переменную экранированные аргументы
    $formattedArgs = ($Arguments | ForEach-Object {
        if ($_ -match '\s|;|=|"') {
            "`"$_`""  # Экранируем аргументы с пробелами/спецсимволами
        } else {
            $_
        }
    }) -join ' '

    Write-Host "$Command $formattedArgs"

    # Сбрасываем последний код возврата
    $LASTEXITCODE = 0

    # Запускаем внешнюю команду
    & $Command $Arguments

    # Проверяем код возврата внешней команды
    if ($LASTEXITCODE -ne 0) {

        throw "Command '$Command $formattedArgs' failed with exit code $LASTEXITCODE."
    }
}

pushd $ProjectDir
Remove-Item -Path tests/coverage/ -Recurse -ErrorAction Ignore
Remove-Item -Path tests/coverage-report/ -Recurse -ErrorAction Ignore
EchoAndCall -- dotnet test --settings tests/tests.runsettings --collect "XPlat Code Coverage" --results-directory=tests/coverage/
EchoAndCall -- dotnet-coverage merge tests/coverage/*/*.xml --output tests/coverage/merged.cobertura.xml --output-format cobertura
EchoAndCall -- reportgenerator "-reports:tests/coverage/merged.cobertura.xml" "-targetdir:tests/coverage-report/" "-reporttypes:Html"
popd