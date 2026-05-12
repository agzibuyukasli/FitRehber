// ═══════════════════════════════════════════════════════════════════════════
//  FitRehber – Jenkins Declarative Pipeline
//  Aşamalar:
//    1. Checkout
//    2. Restore & Build (.NET)
//    3. Build Frontend (Next.js)
//    4. Static Analysis (SonarCloud)
//    5. Start Infrastructure (Docker Compose)
//    6. Selenium UI Tests
//  Temizlik her koşulda post {} bloğu ile yapılır.
// ═══════════════════════════════════════════════════════════════════════════

pipeline {

    // ── Agent ──────────────────────────────────────────────────────────────
    // Herhangi bir Jenkins agent. Agent üzerinde şunlar kurulu olmalı:
    //   .NET 8 SDK, Docker + Compose v2, Google Chrome (headless), curl
    agent any

    // ── Araç Tanımları ─────────────────────────────────────────────────────
    // Jenkins > Manage Jenkins > Tools altında aynı isimle tanımlanmalı.
    tools {
        jdk    'JDK-17'      // SonarScanner için Java 17 zorunlu
        nodejs 'NodeJS-20'   // Next.js build için Node.js 20.x
    }

    // ── Pipeline Seçenekleri ────────────────────────────────────────────────
    options {
        timeout(time: 40, unit: 'MINUTES')              // Toplam max süre
        buildDiscarder(logRotator(numToKeepStr: '10'))  // Son 10 build sakla
        disableConcurrentBuilds()                       // Aynı anda tek build
        timestamps()                                    // Log satırlarına zaman damgası
    }

    // ── Ortam Değişkenleri ──────────────────────────────────────────────────
    environment {
        // .NET CLI sessiz mod
        DOTNET_NOLOGO                    = 'true'
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 'true'

        // SonarCloud (proje ve org bilgileri)
        SONAR_PROJECT_KEY = 'agzibuyukasli_FitRehber'
        SONAR_ORG         = 'agzibuyukasli'
        SONAR_HOST_URL    = 'https://sonarcloud.io'

        // Test ortamı
        TEST_HEADLESS           = 'true'                       // Headless Chrome
        TEST_BASE_URL           = 'http://localhost:3000'      // Frontend URL
        TEST_API_URL            = 'http://localhost:8080'      // Backend API URL
        TEST_ADMIN_EMAIL        = 'admin@fitrehber.com'
        TEST_ADMIN_PASSWORD     = 'Admin123!'
        TEST_DIETITIAN_EMAIL    = 'diyetisyen@fitrehber.com'
        TEST_DIETITIAN_PASSWORD = 'Dietitian@123'
        TEST_TEARDOWN           = 'true'  // Test bitince docker compose down
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  AŞAMALAR
    // ═══════════════════════════════════════════════════════════════════════
    stages {

        // ── 1. CHECKOUT ────────────────────────────────────────────────────
        stage('Checkout') {
            steps {
                // scm: Multibranch Pipeline'da otomatik repo + branch bilgisi alır.
                // Credentials gerektiren (private) repolar için aşağıdaki satırı
                // yorumdan çıkarın ve GIT_CREDENTIALS credential ID'sini tanımlayın:
                //
                //   git branch: env.BRANCH_NAME ?: 'main',
                //       credentialsId: 'GIT_CREDENTIALS',
                //       url: 'https://github.com/agzibuyukasli/FitRehber.git'
                checkout scm

                echo "Branch: ${env.BRANCH_NAME ?: 'main'} | Build: #${env.BUILD_NUMBER}"
            }
        }

        // ── 2. RESTORE & BUILD (.NET) ──────────────────────────────────────
        stage('Restore & Build (.NET)') {
            steps {
                sh '''
                    echo "── .NET Restore ──"
                    dotnet restore DietitianClinicAutomation.sln

                    echo "── .NET Build (Release) ──"
                    dotnet build DietitianClinicAutomation.sln \
                        --no-restore \
                        --configuration Release \
                        -warnaserror:false
                '''
            }
        }

        // ── 3. BUILD FRONTEND (Next.js) ─────────────────────────────────────
        stage('Build Frontend (Next.js)') {
            steps {
                dir('src/DietitianClinic-UI') {
                    sh '''
                        echo "── npm ci ──"
                        npm ci --prefer-offline

                        echo "── next build ──"
                        npm run build
                    '''
                }
            }
        }

        // ── 4. STATIC ANALYSIS (SonarCloud) ────────────────────────────────
        stage('Static Analysis (SonarCloud)') {
            steps {
                withCredentials([string(credentialsId: 'SONAR_TOKEN', variable: 'SONAR_TOKEN_VAL')]) {
                    sh '''
                        # dotnet-sonarscanner global tool kurulumu / güncellemesi
                        dotnet tool install --global dotnet-sonarscanner 2>/dev/null \
                            || dotnet tool update --global dotnet-sonarscanner 2>/dev/null \
                            || true
                        export PATH="$PATH:$HOME/.dotnet/tools"

                        echo "── SonarScanner Begin ──"
                        dotnet sonarscanner begin \
                            /k:"${SONAR_PROJECT_KEY}" \
                            /o:"${SONAR_ORG}" \
                            /d:sonar.host.url="${SONAR_HOST_URL}" \
                            /d:sonar.token="${SONAR_TOKEN_VAL}" \
                            /d:sonar.exclusions="**/obj/**,**/bin/**,**/node_modules/**,.sonarqube/**" \
                            /d:sonar.sourceEncoding="UTF-8"

                        echo "── Build (SonarScanner için) ──"
                        dotnet build DietitianClinicAutomation.sln \
                            --no-restore \
                            --configuration Release

                        echo "── SonarScanner End ──"
                        dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN_VAL}"
                    '''
                }
            }
        }

        // ── 5. START INFRASTRUCTURE (Docker Compose) ───────────────────────
        stage('Start Infrastructure') {
            steps {
                // DOCKER_ENV_FILE credential'ı: .env dosyasının içeriğini tutan
                // Jenkins "Secret File" credential. Bu adımda geçici olarak kopyalanır.
                withCredentials([file(credentialsId: 'DOCKER_ENV_FILE', variable: 'DOCKER_ENV')]) {
                    sh 'cp "$DOCKER_ENV" .env'
                }

                sh '''
                    echo "── docker compose up ──"
                    docker compose up -d

                    # ── Backend API hazır olana kadar bekle (max 3 dk) ──────
                    echo "Backend API bekleniyor: ${TEST_API_URL}/api/health/database"
                    READY=0
                    for i in $(seq 1 60); do
                        if curl -sf "${TEST_API_URL}/api/health/database" > /dev/null 2>&1; then
                            echo "Backend API hazır. (deneme: $i)"
                            READY=1
                            break
                        fi
                        echo "  Bekleniyor... ($i/60)"
                        sleep 3
                    done
                    if [ "$READY" -eq 0 ]; then
                        echo "HATA: Backend API 3 dakika içinde başlamadı!" >&2
                        docker compose logs api
                        exit 1
                    fi

                    # ── Frontend hazır olana kadar bekle (max 2 dk) ──────────
                    echo "Frontend bekleniyor: ${TEST_BASE_URL}"
                    READY=0
                    for i in $(seq 1 40); do
                        if curl -sf "${TEST_BASE_URL}" > /dev/null 2>&1; then
                            echo "Frontend hazır. (deneme: $i)"
                            READY=1
                            break
                        fi
                        echo "  Bekleniyor... ($i/40)"
                        sleep 3
                    done
                    if [ "$READY" -eq 0 ]; then
                        echo "HATA: Frontend 2 dakika içinde başlamadı!" >&2
                        docker compose logs frontend
                        exit 1
                    fi

                    echo "Tüm servisler hazır."
                '''
            }
        }

        // ── 6. SELENIUM UI TESTS ────────────────────────────────────────────
        stage('Selenium UI Tests') {
            steps {
                sh '''
                    mkdir -p TestResults

                    dotnet test src/DietitianClinic.Tests.UI/DietitianClinic.Tests.UI.csproj \
                        --no-build \
                        --configuration Release \
                        --logger "trx;LogFileName=selenium-results.trx" \
                        --logger "console;verbosity=normal" \
                        --results-dir "${WORKSPACE}/TestResults"
                '''
            }
            post {
                always {
                    // TRX (MSTest/NUnit) sonuçlarını Jenkins'e yayımla.
                    // "JUnit" plugin TRX formatını destekler.
                    junit allowEmptyResults: true,
                          testResults: 'TestResults/*.trx'
                }
            }
        }

    } // end stages

    // ═══════════════════════════════════════════════════════════════════════
    //  POST – Her koşulda çalışan temizlik ve bildirimler
    // ═══════════════════════════════════════════════════════════════════════
    post {

        always {
            script {
                // Docker servislerini durdur (hata olsa bile devam et)
                try {
                    sh 'docker compose down --volumes --remove-orphans'
                } catch (err) {
                    echo "docker compose down sırasında hata (yoksayılıyor): ${err}"
                }

                // .env dosyasını sil (gizli bilgileri temizle)
                try {
                    sh 'rm -f .env'
                } catch (err) {
                    echo ".env silinirken hata (yoksayılıyor): ${err}"
                }
            }

            // Workspace'i temizle
            cleanWs()
        }

        success {
            echo "✅ Pipeline başarıyla tamamlandı. Branch: ${env.BRANCH_NAME ?: 'main'} | Build: #${env.BUILD_NUMBER}"
        }

        failure {
            echo "❌ Pipeline başarısız. Branch: ${env.BRANCH_NAME ?: 'main'} | Build: #${env.BUILD_NUMBER}"
        }

        unstable {
            echo "⚠️ Pipeline kararsız (bazı testler başarısız veya analiz uyarıları mevcut)."
        }

    }

} // end pipeline
