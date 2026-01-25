import pandas as pd
import matplotlib.pyplot as plt
import numpy as np

# 1. VERİYİ YÜKLEME
csv_path = 'learning_results.csv' 

try:
    df = pd.read_csv(csv_path)
    # Sütun isimlerindeki boşlukları temizle
    df.columns = df.columns.str.strip()
    print("Veri yüklendi. Kontrol ediliyor...")

except Exception as e:
    print(f"Hata: Dosya okunamadı. {e}")
    exit()

# 2. VERİ TEMİZLEME VE DÜZELTME (Otomatik Kayma Düzeltici)
# HGAScore sütunu sayısal değilse (örn: 'Q-Learning' yazıyorsa) veriler kaymış demektir.
is_data_shifted = False
try:
    pd.to_numeric(df['HGAScore'])
except ValueError:
    is_data_shifted = True

if is_data_shifted:
    print("UYARI: Sütunlarda kayma tespit edildi. Veriler düzeltiliyor...")
    
    # Yeni ve temiz bir DataFrame oluşturalım
    clean_df = pd.DataFrame()
    
    # 1. Sütundaki veriler aslında Q-Learning Skoru (GameNumber başlığı altında kalmış)
    clean_df['QLearningScore'] = pd.to_numeric(df.iloc[:, 0], errors='coerce')
    
    # 2. Sütundaki veriler aslında HGA Skoru (QLearningScore başlığı altında kalmış)
    clean_df['HGAScore'] = pd.to_numeric(df.iloc[:, 1], errors='coerce')
    
    # 3. Sütundaki veriler aslında Kazanan (HGAScore başlığı altında kalmış)
    clean_df['Winner'] = df.iloc[:, 2]
    
    # Epsilon (Muhtemelen 4. veya 5. sütunda, 1000 ile çarpılmış halde)
    # Sütun başlığı 'QLearningEpsilon' olanı bulmaya çalışalım veya index ile alalım
    # Veri örneğine göre 5. sütun (index 4) gibi görünüyor: 199
    try:
        clean_df['QLearningEpsilon'] = pd.to_numeric(df.iloc[:, 4], errors='coerce') / 1000.0
    except:
        clean_df['QLearningEpsilon'] = 0
        
    # Genetik Nesil (Index 5)
    clean_df['GAGeneration'] = pd.to_numeric(df.iloc[:, 5], errors='coerce')
    
    # En İyi Fitness (Index 6)
    clean_df['GABestFitness'] = pd.to_numeric(df.iloc[:, 6], errors='coerce')

    # Oyun Numarası (Sırayla 1'den başlatalım)
    clean_df['GameNumber'] = range(1, len(clean_df) + 1)
    
    # Ana DataFrame'i güncelleyelim
    df = clean_df
    print("Veriler başarıyla onarıldı!")

else:
    print("Veri formatı düzgün görünüyor.")

# 3. GRAFİKLERİ ÇİZME

# Grafik Ayarları
plt.style.use('ggplot') # Daha şık görünüm

# --- GRAFİK 1: SKOR GELİŞİMİ ---
plt.figure(figsize=(12, 6))
window_size = 5 # Hareketli ortalama penceresi

# Rolling mean hesaplarken hata almamak için veriyi float yapalım
df['QLearningScore'] = df['QLearningScore'].astype(float)
df['HGAScore'] = df['HGAScore'].astype(float)

plt.plot(df['GameNumber'], df['QLearningScore'].rolling(window=window_size).mean(), label='Q-Learning (Trend)', color='blue', linewidth=2)
plt.plot(df['GameNumber'], df['HGAScore'].rolling(window=window_size).mean(), label='HGA (Trend)', color='red', linewidth=2)

# Saydam gerçek veriler
plt.scatter(df['GameNumber'], df['QLearningScore'], alpha=0.2, color='blue', s=10)
plt.scatter(df['GameNumber'], df['HGAScore'], alpha=0.2, color='red', s=10)

plt.title(f'YZ Skor Gelişimi (Toplam {len(df)} Oyun)')
plt.xlabel('Oyun Sayısı')
plt.ylabel('Skor (Hareketli Ortalama)')
plt.legend()
plt.tight_layout()
plt.savefig('grafik_1_skor_gelisimi.png')
print("- grafik_1_skor_gelisimi.png kaydedildi.")

# --- GRAFİK 2: Q-LEARNING ANALİZİ ---
fig, ax1 = plt.subplots(figsize=(10, 6))

color = 'tab:blue'
ax1.set_xlabel('Oyun Sayısı')
ax1.set_ylabel('Skor', color=color)
ax1.plot(df['GameNumber'], df['QLearningScore'], color=color, alpha=0.6, label='Skor')
ax1.tick_params(axis='y', labelcolor=color)

ax2 = ax1.twinx() 
color = 'tab:orange'
ax2.set_ylabel('Epsilon (Keşif)', color=color)
ax2.plot(df['GameNumber'], df['QLearningEpsilon'], color=color, linestyle='--', linewidth=2, label='Epsilon')
ax2.tick_params(axis='y', labelcolor=color)

plt.title('Q-Learning: Keşif Oranı vs Başarı')
fig.tight_layout()
plt.savefig('grafik_2_qlearning_analiz.png')
print("- grafik_2_qlearning_analiz.png kaydedildi.")

# --- GRAFİK 3: KAZANAN DAĞILIMI ---
plt.figure(figsize=(6, 6))
if 'Winner' in df.columns:
    win_counts = df['Winner'].value_counts()
    plt.pie(win_counts, labels=win_counts.index, autopct='%1.1f%%', colors=['#ff9999','#66b3ff'])
    plt.title('Kazanma Oranları')
    plt.savefig('grafik_3_kazanma_oranlari.png')
    print("- grafik_3_kazanma_oranlari.png kaydedildi.")

# --- GRAFİK 4: GENETİK ALGORİTMA FITNESS ---
plt.figure(figsize=(10, 5))
# Nesil başına en yüksek skoru bul
if 'GAGeneration' in df.columns and 'GABestFitness' in df.columns:
    gen_data = df.groupby('GAGeneration')['GABestFitness'].max()
    plt.plot(gen_data.index, gen_data.values, marker='o', linestyle='-', color='green')
    plt.title('Genetik Algoritma: Nesillere Göre En İyi Fitness')
    plt.xlabel('Nesil')
    plt.ylabel('Fitness Puanı')
    plt.grid(True)
    plt.savefig('grafik_4_ga_fitness.png')
    print("- grafik_4_ga_fitness.png kaydedildi.")

plt.show()
print("İşlem tamamlandı.")