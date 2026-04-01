
---

# 🤖 AI 아카데미 LLM & 에이전트 독학 커리큘럼

## 🏗️ [모듈 1] 모델 최적화 및 고도화 (The Brain Builder)
모델의 구조를 이해하고, 직접 학습시키며 성능을 한계까지 끌어올리는 과정입니다.

### 🎯 학습 목표
* [cite_start]오픈소스 LLM(Llama, Gemma 등)의 구조적 특징 이해 [cite: 8]
* [cite_start]파인튜닝(Fine-tuning) 및 강화학습(RLHF)을 통한 모델 정렬 [cite: 186]
* [cite_start]모델 압축 및 온디바이스(On-device) 최적화 기술 습득 [cite: 326]

### 📑 커리큘럼 세부 내용
* **Part 1. 파인튜닝 기초와 심화**
    * [cite_start]PEFT/LoRA 개념 이해 및 QA 데이터셋 활용 실습 [cite: 8]
    * [cite_start]양자화(PTQ, QAT) 및 QLoRA를 이용한 모델 경량화 [cite: 8, 262]
    * [cite_start]훈련된 모델의 Robustness 확보 및 프로덕션 배포 전략 [cite: 262]
* **Part 2. 인간 피드백 기반 강화학습(RLHF)**
    * [cite_start]SFT(지도 학습)와 RLHF의 정의 및 흐름 파악 [cite: 186]
    * [cite_start]PPO, DPO, ORPO 등 주요 강화학습 알고리즘 이론 및 실습 [cite: 186]
    * [cite_start]최신 GRPO(Deepseek), GSPO(Qwen) 알고리즘 분석 [cite: 186]
* **Part 3. 지식증류 및 온디바이스 LLM**
    * [cite_start]Teacher-Student 구조를 활용한 지식증류(Knowledge Distillation) [cite: 326]
    * [cite_start]TinyLlama, MobileLLM 등 온디바이스 전용 모델의 특징 이해 [cite: 326]
    * [cite_start]BitNet을 활용한 추론 가속 및 로컬 환경 탑재 실습 [cite: 326]

---

## 📚 [모듈 2] 데이터 연결 및 검색 아키텍처 (The Knowledge Library)
LLM이 외부 지식을 활용하여 정확한 답변을 내놓게 하는 '연결'의 기술을 다룹니다.

### 🎯 학습 목표
* [cite_start]RAG(Retrieval-Augmented Generation) 아키텍처 설계 및 구현 [cite: 232, 251]
* [cite_start]검색 엔진과 LLM의 유기적 결합을 통한 지식 기반 서비스 구축 [cite: 232]

### 📑 커리큘럼 세부 내용
* **Part 1. 랭체인과 RAG 서비스 개발**
    * [cite_start]LangChain 프레임워크의 개념 및 구성 요소 이해 [cite: 251]
    * [cite_start]PDF/CSV 데이터 로드 및 텍스트 요약 웹사이트 제작 [cite: 251]
    * [cite_start]Streamlit을 활용한 대화형 챗봇 및 데이터 시각화 구현 [cite: 251]
* **Part 2. 엘라스틱서치와 벡터 검색**
    * [cite_start]역인덱스 기반 자연어 검색과 토크나이저(Mecab-ko, Nori) 원리 [cite: 232]
    * [cite_start]임베딩 벡터 기반의 의미(Semantic) 검색 및 유사도 계산 [cite: 232]
    * [cite_start]HNSW 알고리즘을 활용한 대규모 벡터 검색 성능 튜닝 [cite: 232]
* **Part 3. 하이브리드 검색 전략**
    * [cite_start]키워드(BM25)와 벡터 검색을 결합한 하이브리드 전략 수립 [cite: 232]
    * [cite_start]검색 결과 재정렬(Reranking)을 통한 답변 정확도 향상 [cite: 232]

---

## 🤖 [모듈 3] 자율 에이전트 및 워크플로우 (The Autonomous Worker)
스스로 도구를 사용하고 문제를 해결하는 'AI 비서'를 만드는 실전 과정입니다.

### 🎯 학습 목표
* [cite_start]자율 에이전트(Autonomous Agent)의 아키텍처 이해 및 구축 [cite: 118, 204]
* [cite_start]AI-Native 개발 환경(IDE)을 활용한 연구 생산성 극대화 [cite: 118]

### 📑 커리큘럼 세부 내용
* **Part 1. Agentic R&D 혁명**
    * [cite_start]Reasoning Model의 부상과 에이전틱 아키텍처의 필요성 [cite: 118]
    * [cite_start]Google Antigravity(AI-Native IDE)를 활용한 다중 에이전트 관리 [cite: 118]
    * [cite_start]자율 디버깅 및 에러 스스로 수정하기(Self-Healing) 실습 [cite: 118]
* **Part 2. MCP 에이전트 실전 구축**
    * [cite_start]Claude와 MCP(Model Context Protocol)를 활용한 도구 연결 [cite: 204]
    * [cite_start]구글 워크스페이스, 노션, 엑셀 연동을 통한 업무 자동화 [cite: 204]
    * [cite_start]유튜브 스크립트 추출 및 트렌드 리포트 자동 작성 [cite: 204]
* **Part 3. 실무형 프로젝트**
    * [cite_start]엑셀 데이터를 활용한 재고 관리 및 금융 데이터 분석 대시보드 [cite: 204]
    * [cite_start]AI 기반 웹사이트 구축 및 배포(점심 메뉴 추천 앱 등) [cite: 204]

---

## ⚖️ [모듈 4] 도구 활용 및 성능 검증 (The Practice & Test)
최신 도구로 빠르게 실험하고, 그 결과가 정말 믿을만한지 과학적으로 검증합니다.

### 🎯 학습 목표
* [cite_start]최신 LLM 학습 및 추론 가속 도구 마스터 [cite: 51]
* [cite_start]벤치마크 및 지표를 활용한 모델 성능의 객관적 평가 [cite: 90]

### 📑 커리큘럼 세부 내용
* **Part 1. LLM 학습 및 추론 툴 마스터**
    * [cite_start]Unsloth 기반의 초고속 파인튜닝 환경 구축 및 성능 비교 [cite: 51]
    * [cite_start]Llama-factory, Axolotl, TorchTune 등 도구별 특징 분석 [cite: 51]
    * [cite_start]레시피 기반의 효율적인 학습 및 추론 파이프라인 구성 [cite: 51]
* **Part 2. LLM 평가의 이론과 실제**
    * [cite_start]자동 정량 평가 지표(BLEU, ROUGE, BERTScore 등) 이해 [cite: 90]
    * [cite_start]MMLU, GSM8K 등 주요 벤치마크 데이터셋 분석 [cite: 90]
    * [cite_start]LLM-as-a-Judge: GPT-4 등을 활용한 정성 평가 자동화 [cite: 90]
* **Part 3. AI 코딩 프로젝트 체험**
    * [cite_start]ChatGPT, Cursor AI를 활용한 코딩 보조 기능 실습 [cite: 225]
    * [cite_start]대화형 모델과 협업하여 실제 프로젝트 결과물 도출 및 비교 [cite: 225]


## [cite_start]🔐 [모듈 5] 시계열 분석 및 보안 응용 (Time-Series & Security) [cite: 285, 286]
부채널 신호 분석 및 암호 보안 연구를 위한 시계열 데이터 처리 전문 과정입니다.

### [cite_start]🎯 학습 목표 [cite: 286]
* 시계열 데이터 핵심 개념 습득 및 머신러닝 예측 모델 구축
* CNN, RNN(LSTM) 딥러닝을 이용한 신호 데이터 특징 추출
* Prophet 라이브러리를 활용한 대규모 데이터 분석 역량 확보

### [cite_start]📑 커리큘럼 세부 내용 [cite: 286]
* **Part 1. 신호 전처리 및 분석 기초**: 시계열 구조 이해, EDA, 평활화(Smoothing), 필터링, 요소분해 실습
* **Part 2. 통계적 예측 모델링**: 자기회귀(AR), 이동 평균법(MA), ARIMA 및 계절성을 고려한 SARIMA 모델 활용
* **Part 3. 딥러닝 기반 특징 학습**: CNN을 활용한 시계열 필터링, LSTM 기반의 순차 데이터 모델 학습 및 결과 분석
* **Part 4. 실전 분석 라이브러리**: Prophet 라이브러리 구조 이해 및 대규모 시계열 데이터 예측 처리


# 🔐 [모듈 6] PQC 임베디드 구현을 위한 하드웨어 기초 체력 (Nucleo M3 최적화 가이드)

[cite_start]본 모듈은 **[SE-B1001]**의 핵심 내용을 연구원님의 **Nucleo M3(Cortex-M3) 기반 PQC 구현** 시나리오에 맞춰 재설계한 것입니다. [cite: 192, 194]

### 🎯 학습 목표
* [cite_start]Nucleo M3 보드의 내부 구조(SoC/MCU)를 이해하여 PQC 알고리즘의 실행 흐름 파악 [cite: 194]
* [cite_start]PQC의 거대한 키(Key)와 서명을 수용하기 위한 메모리 맵(Memory Map) 설계 능력 배양 [cite: 194, 197]
* [cite_start]하드웨어 초기화 및 레지스터 제어 원리를 습득하여 자체적인 디버깅 기반 마련 [cite: 194, 198]

---

### 📑 커리큘럼 세부 내용

#### **Part 1. 임베디드 구조와 PQC의 메모리 레이아웃**
* [cite_start]**SoC/MCU 및 Register 이해**: MCU 내부의 연산 장치와 데이터가 잠시 머무는 '레지스터'의 개념을 익힙니다. [cite: 194] [cite_start]이는 PQC의 복잡한 연산이 실제 하드웨어에서 어떻게 처리되는지 이해하는 기초가 됩니다. [cite: 194, 197]
* [cite_start]**Memory Map과 MMIO (Memory Mapped I/O)**: 보드의 어느 주소가 Flash(코드 저장소)이고 어느 주소가 RAM(변수 저장소)인지 구분하는 법을 배웁니다. [cite: 194, 197] [cite_start]PQC의 거대한 파라미터를 메모리에 효율적으로 배치하기 위해 반드시 알아야 할 개념입니다. [cite: 194]

#### **Part 2. 펌웨어 동작 원리와 초기화 (Startup Process)**
* [cite_start]**하드웨어 초기화 요소**: 전원이 켜진 후 `main()` 함수가 실행되기 전까지, 시스템 클럭과 전원이 어떻게 설정되는지 배웁니다. [cite: 194] [cite_start]보드에 코드를 올렸을 때 아무 반응이 없는 상황을 해결하기 위한 필수 지식입니다. [cite: 194]
* [cite_start]**Memory 종류와 캐시(Cache)**: PQC 연산 속도에 직접적인 영향을 주는 메모리 계층 구조와 캐시 활용의 기초를 다룹니다. [cite: 194]

#### **Part 3. ARM Cortex-M3 아키텍처 및 최적화 기초**
* [cite_start]**ARM 프로세서 기본 구조**: 연구원님이 사용하시는 Nucleo M3의 두뇌인 ARM 코어의 특징을 이해합니다. [cite: 194]
* [cite_start]**명령어(Assembly)와 최적화**: C 코드가 실제 어떤 기계어로 변환되는지 살펴봅니다. [cite: 194] [cite_start]암호 연산 루프를 최적화하거나, 성능 병목 구간을 찾을 때 AI와 대화하기 위한 '공통 언어'가 됩니다. [cite: 194]

#### **Part 4. 실무 디버깅을 위한 하드웨어 제어**
* [cite_start]**GPIO 및 디바이스 제어**: LED를 깜빡이거나 UART 통신으로 결과값을 출력하는 등, 암호 구현 결과나 에러 로그를 눈으로 확인하기 위한 가장 기초적인 하드웨어 조작법을 익힙니다. [cite: 194, 198]

---

### [cite_start]🛠️ 교육 및 실습 환경 [cite: 194]
* [cite_start]**기본 언어**: C 프로그래밍 [cite: 194]
* [cite_start]**실습 장비**: ARM 기반 Reference Board (Nucleo M3와 유사한 환경), Ubuntu, GNU Toolchain [cite: 194]
* [cite_start]**강사**: 박성호 (한컴이노스트림 부장) [cite: 194]

