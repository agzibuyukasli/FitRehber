pipeline {
    agent any

    tools {
        jdk 'JDK-17'
        nodejs 'NodeJS-20'
    }

    options {
        timeout(time: 40, unit: 'MINUTES')
        timestamps()
    }

    environment {
        DOTNET_NOLOGO = 'true'
        SONAR_PROJECT_KEY = 'agzibuyukasli_FitRehber'
        SONAR_ORG = 'agzibuyukasli'
        SONAR_HOST_URL = 'https://sonarcloud.io'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
                echo "Kodlar başarıyla çekildi."
            }
        }

        stage('Restore & Build (.NET)') {
            steps {
                bat '''
                    @echo off
                    echo === .NET Restore ===
                    dotnet restore DietitianClinicAutomation.sln
                    
                    echo === .NET Build Release ===
                    dotnet build DietitianClinicAutomation.sln --no-restore --configuration Release /maxcpucount:1 -p:UseSharedCompilation=false
                '''
            }
        }

        stage('Static Analysis (SonarCloud)') {
            steps {
                // SonarCloud için şifre gerekecektir, şimdilik sadece echo bırakıyorum.
                // İleride credentials ekleyince burayı aktif edersin.
                echo "SonarCloud analizi için altyapı hazır."
            }
        }
    }

    post {
        always {
            cleanWs()
        }
        success {
            echo "TEBRİKLER ASLI! 65 hata çözüldü ve Pipeline başarıyla çalıştı! 🚀"
        }
        failure {
            echo "Hala bir sorun var, ama en azından syntax hatası değil! Loglara bak kanka."
        }
    }
}