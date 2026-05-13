pipeline {
    agent any
    
    tools {
        jdk 'JDK-17'
        nodejs 'NodeJS-20'
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
                bat 'dotnet build DietitianClinicAutomation.sln --no-restore --configuration Release /maxcpucount:1 -p:UseSharedCompilation=false'
            }
        }

        stage('Build Frontend (Next.js)') {
            steps {
                // UI klasörüne girip işlemleri orada yapıyoruz
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
    }
    
    post {
        always {
            cleanWs()
        }
        success {
            echo 'Backend ve Frontend başarıyla derlendi! 🚀'
        }
    }
}