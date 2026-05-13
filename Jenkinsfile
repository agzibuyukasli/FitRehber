// FitRehber - Jenkins Declarative Pipeline (Windows CMD uyumlu)
// Tum shell adimlari bat (Windows Command Prompt) olarak yazilmistir.
// Agent uzerinde kurulu olmasi gerekenler:
//   .NET 8 SDK | Node.js 20.x | Docker + Compose v2
//   Google Chrome (headless Selenium icin) | curl (Win10+ yerlesik)

pipeline {

    // Agent
    agent any

    // Arac Tanimlari
    // Jenkins > Manage Jenkins > Tools altinda ayni isimle tanimlanmali.
    tools {
        jdk    'JDK-17'     // SonarScanner icin Java 17 zorunlu
        nodejs 'NodeJS-20'  // Next.js build icin Node.js 20.x
    }

    // Pipeline Secenekleri
    options {
        timeout(time: 40, unit: 'MINUTES')
        buildDiscarder(logRotator(numToKeepStr: '10'))
        disableConcurrentBuilds()
        timestamps()
    }

    // Ortam Degiskenleri
    environment {
        DOTNET_NOLOGO                     = 'true'
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 'true'

        // SonarCloud
        SONAR_PROJECT_KEY = 'agzibuyukasli_FitRehber'
        SONAR_ORG         = 'agzibuyukasli'
        SONAR_HOST_URL    = 'https://sonarcloud.io'

        // Selenium test ortami
        TEST_HEADLESS           = 'true'
        TEST_BASE_URL           = 'http://localhost:3000'
        TEST_API_URL            = 'http://localhost:8080'
        TEST_ADMIN_EMAIL        = 'admin@fitrehber.com'
        TEST_ADMIN_PASSWORD     = 'Admin123!'
        TEST_DIETITIAN_EMAIL    = 'diyetisyen@fitrehber.com'
        TEST_DIETITIAN_PASSWORD = 'Dietitian@123'
        TEST_TEARDOWN           = 'true'
    }

    stages {

        // 1. CHECKOUT
        stage('Checkout') {
            steps {
                // Public repo: checkout scm yeterli.
                // Private repo icin asagidaki satirlari yorumdan cikarin:
                //   git branch: env.BRANCH_NAME ?: 'main',
                //       credentialsId: 'GIT_CREDENTIALS',
                //       url: 'https://github.com/agzibuyukasli/FitRehber.git'
                checkout scm
                echo "Branch: ${env.BRANCH_NAME ?: 'main'} | Build: #${env.BUILD_NUMBER}"
            }
        }

        // 2. RESTORE & BUILD (.NET)
        stage('Restore & Build (.NET)') {
            steps {
                bat '''
                    @echo off
                    echo === .NET Restore ===
                    dotnet restore DietitianClinicAutomation.sln
                    if %ERRORLEVEL% NEQ 0 (
                        echo HATA: dotnet restore basarisiz!
                        exit /b %ERRORLEVEL%
                    )

                    echo === .NET Build Release ===
                    rem /maxcpucount:1 ve UseSharedCompilation=false: Roslyn OutOfMemory hatasini onler.
                    dotnet build DietitianClinicAutomation.sln --no-restore --configuration Release -warnaserror:false /maxcpucount:1 -p:UseSharedCompilation=false
                    if %ERRORLEVEL% NEQ 0 (
                        echo HATA: dotnet build basarisiz!
                        exit /b %ERRORLEVEL%
                    )
                '''
            }
        }

        // 3. BUILD FRONTEND (Next.js)
        // dir() icinde / (egik cizgi) kullanilir; Jenkins Windows'ta da kabul eder.
        stage('Build Frontend (Next.js)') {
            steps {
                dir('src/DietitianClinic-UI') {
                    bat '''
                        @echo off
                        echo === npm ci ===
                        npm ci --prefer-offline
                        if %ERRORLEVEL% NEQ 0 (
                            echo HATA: npm ci basarisiz!
                            exit /b %ERRORLEVEL%
                        )

                        echo === next build ===
                        npm run build
                        if %ERRORLEVEL% NEQ 0 (
                            echo HATA: next build basarisiz!
                            exit /b %ERRORLEVEL%
                        )
                    '''
                }
            }
        }

        // 4. STATIC ANALYSIS (SonarCloud)
        stage('Static Analysis (SonarCloud)') {
            steps {
                withCredentials([string(credentialsId: 'SONAR_TOKEN', variable: 'SONAR_TOKEN_VAL')]) {
                    bat '''
                        @echo off
                        echo === Tool Setup ===
                        dotnet tool install --global dotnet-sonarscanner >nul 2>&1 || dotnet tool update --global dotnet-sonarscanner >nul 2>&1

                        echo === Path Configuration ===
                        set PATH=%PATH%;%USERPROFILE%\\.dotnet\\tools

                        echo === SonarScanner Begin ===
                        dotnet sonarscanner begin ^
                            /k:"%SONAR_PROJECT_KEY%" ^
                            /o:"%SONAR_ORG%" ^
                            /d:sonar.host.url="%SONAR_HOST_URL%" ^
                            /d:sonar.token="%SONAR_TOKEN_VAL%" ^
                            /d:sonar.exclusions="**/obj/**,**/bin/**,**/node_modules/**,.sonarqube/**" ^
                            /d:sonar.sourceEncoding="UTF-8"

                        if %ERRORLEVEL% NEQ 0 (
                            echo HATA: SonarScanner begin basarisiz!
                            exit /b %ERRORLEVEL%
                        )

                        echo === Build Application ===
                        dotnet build DietitianClinicAutomation.sln --no-restore --configuration Release /maxcpucount:1 -p:UseSharedCompilation=false

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

        // 5. START INFRASTRUCTURE (Docker Compose)
        stage('Start Infrastructure') {
            steps {
                // DOCKER_ENV_FILE: Jenkins "Secret File" credential.
                // .env.example dosyasini doldurup Jenkins'e "Secret File" olarak ekleyin.
                withCredentials([file(credentialsId: 'DOCKER_ENV_FILE', variable: 'DOCKER_ENV')]) {
                    bat 'copy /Y "%DOCKER_ENV%" .env'
                }

                bat '''
                    @echo off
                    echo === docker compose up ===
                    docker compose up -d
                    if %ERRORLEVEL% NEQ 0 (
                        echo HATA: docker compose up basarisiz!
                        exit /b %ERRORLEVEL%
                    )
                '''

                // Backend API hazir olana kadar bekle (maks. 3 dakika = 60 x 3sn)
                bat '''
                    @echo off
                    setlocal enabledelayedexpansion

                    echo Backend API bekleniyor...
                    set ATTEMPTS=60

                    :API_WAIT
                    if !ATTEMPTS! EQU 0 goto API_TIMEOUT
                    curl -sf %TEST_API_URL%/api/health/database >nul 2>&1
                    if %ERRORLEVEL% EQU 0 goto API_READY
                    echo   Bekleniyor... kalan deneme: !ATTEMPTS!
                    timeout /t 3 /nobreak >nul
                    set /a ATTEMPTS-=1
                    goto API_WAIT

                    :API_TIMEOUT
                    echo HATA: Backend API 3 dakika icinde baslamadi!
                    docker compose logs api
                    exit /b 1

                    :API_READY
                    echo Backend API hazir.
                    endlocal
                '''

                // Frontend hazir olana kadar bekle (maks. 2 dakika = 40 x 3sn)
                bat '''
                    @echo off
                    setlocal enabledelayedexpansion

                    echo Frontend bekleniyor...
                    set ATTEMPTS=40

                    :FE_WAIT
                    if !ATTEMPTS! EQU 0 goto FE_TIMEOUT
                    curl -sf %TEST_BASE_URL% >nul 2>&1
                    if %ERRORLEVEL% EQU 0 goto FE_READY
                    echo   Bekleniyor... kalan deneme: !ATTEMPTS!
                    timeout /t 3 /nobreak >nul
                    set /a ATTEMPTS-=1
                    goto FE_WAIT

                    :FE_TIMEOUT
                    echo HATA: Frontend 2 dakika icinde baslamadi!
                    docker compose logs frontend
                    exit /b 1

                    :FE_READY
                    echo Frontend hazir.
                    echo Tum servisler hazir.
                    endlocal
                '''
            }
        }

        // 6. SELENIUM UI TESTS
        // NOT: dotnet CLI Windows'ta da / (egik cizgi) kabul eder.
        //      Bu sekilde Groovy'nin \ escape yorumlamasindan tamamen kacinilir.
        stage('Selenium UI Tests') {
            steps {
                bat '''
                    @echo off
                    if not exist TestResults mkdir TestResults

                    dotnet test src/DietitianClinic.Tests.UI/DietitianClinic.Tests.UI.csproj ^
                        --no-build ^
                        --configuration Release ^
                        --logger "trx;LogFileName=selenium-results.trx" ^
                        --logger "console;verbosity=normal" ^
                        --results-dir "%WORKSPACE%/TestResults"
                    exit /b %ERRORLEVEL%
                '''
            }
            post {
                always {
                    // junit testResults forward slash ile de calisir (Jenkins glob)
                    junit allowEmptyResults: true,
                          testResults: 'TestResults/*.trx'
                }
            }
        }

    } // end stages

    // POST: Her kosulda calisan temizlik ve bildirimler
    post {

        always {
            script {
                // Docker servislerini durdur - hata olsa bile devam et
                try {
                    bat 'docker compose down --volumes --remove-orphans'
                } catch (err) {
                    echo "docker compose down hatasi (yoksayiliyor): ${err}"
                }

                // .env dosyasini sil (gizli verileri temizle)
                try {
                    bat 'if exist .env del /F /Q .env'
                } catch (err) {
                    echo ".env silinemedi (yoksayiliyor): ${err}"
                }
            }

            // Workspace temizle
            cleanWs()
        }

        success {
            echo "Pipeline basariyla tamamlandi. Branch: ${env.BRANCH_NAME ?: 'main'} | Build: #${env.BUILD_NUMBER}"
        }

        failure {
            echo "Pipeline basarisiz. Branch: ${env.BRANCH_NAME ?: 'main'} | Build: #${env.BUILD_NUMBER}"
        }

        unstable {
            echo "Pipeline kararssiz: bazi testler basarisiz veya analiz uyarilari mevcut."
        }

    }

} // end pipeline
