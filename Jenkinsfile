pipeline {
    agent any

    tools {
        jdk    'JDK-17'
        nodejs 'NodeJS-20'
    }

    environment {
        // SonarScanner (Java tabanli) icin heap siniri; insufficient memory hatasini onler
        SONAR_SCANNER_OPTS = '-Xmx1024m'
    }

    stages {

        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Restore & Build (.NET)') {
            steps {
                bat 'dotnet restore DietitianClinicAutomation.sln'
                bat 'dotnet build DietitianClinicAutomation.sln --no-restore --configuration Release /maxcpucount:1 -p:UseSharedCompilation=false -p:NoWarn=1591'
            }
        }

        stage('Build Frontend (Next.js)') {
            steps {
                dir('src/DietitianClinic-UI') {
                    bat '''
                        @echo off
                        echo === Node version ===
                        node -v
                        echo === Installing Dependencies ===
                        npm ci --prefer-offline
                        echo === Running Next.js Build ===
                        npm run build
                    '''
                }
            }
        }

        stage('Run Tests') {
            steps {
                catchError(buildResult: 'SUCCESS', stageResult: 'UNSTABLE') {
                    bat '''
                        @echo off
                        echo === Testler calistiriliyor (Coverage koleksiyonu aktif) ===
                        dotnet test DietitianClinicAutomation.sln ^
                            --no-build ^
                            --configuration Release ^
                            --collect:"XPlat Code Coverage" ^
                            --results-directory "%WORKSPACE%\\TestResults" ^
                            --logger "trx;LogFileName=test-results.trx"
                        if %ERRORLEVEL% NEQ 0 (
                            echo UYARI: Bazi testler basarisiz oldu, devam ediliyor.
                            exit /b %ERRORLEVEL%
                        )
                    '''
                }
            }
        }

        stage('Static Analysis (SonarCloud)') {
            steps {
                withCredentials([string(credentialsId: 'SONAR_TOKEN', variable: 'SONAR_TOKEN_VAL')]) {
                    bat '''
                        @echo off

                        echo === dotnet-sonarscanner kurulum/guncelleme ===
                        dotnet tool install --global dotnet-sonarscanner >nul 2>&1 || dotnet tool update --global dotnet-sonarscanner >nul 2>&1

                        echo === PATH guncelleme ===
                        set PATH=%PATH%;%USERPROFILE%\\.dotnet\\tools

                        echo === SonarScanner Begin ===
                        dotnet sonarscanner begin ^
                            /k:"agzibuyukasli_FitRehber" ^
                            /o:"agzibuyukasli" ^
                            /d:sonar.host.url="https://sonarcloud.io" ^
                            /d:sonar.token="%SONAR_TOKEN_VAL%" ^
                            /d:sonar.exclusions="**/obj/**,**/bin/**,**/node_modules/**,.sonarqube/**" ^
                            /d:sonar.sourceEncoding="UTF-8" ^
                            /d:sonar.cs.opencover.reportsPaths="%WORKSPACE%\\TestResults\\**\\coverage.opencover.xml" ^
                            /d:sonar.cs.cobertura.reportsPaths="%WORKSPACE%\\TestResults\\**\\coverage.cobertura.xml"
                        if %ERRORLEVEL% NEQ 0 (
                            echo HATA: SonarScanner begin basarisiz!
                            exit /b %ERRORLEVEL%
                        )

                        echo === Build (SonarCloud icin) ===
                        dotnet build DietitianClinicAutomation.sln --no-restore --configuration Release /maxcpucount:1 -p:UseSharedCompilation=false -p:NoWarn=1591
                        if %ERRORLEVEL% NEQ 0 (
                            echo HATA: dotnet build basarisiz!
                            exit /b %ERRORLEVEL%
                        )

                        echo === SonarScanner End ===
                        dotnet sonarscanner end /d:sonar.token="%SONAR_TOKEN_VAL%"
                    '''
                }
            }
        }

    }

    post {
        always {
            cleanWs()
        }
        success {
            echo 'Backend, Frontend ve SonarCloud analizi basariyla tamamlandi!'
        }
    }
}
