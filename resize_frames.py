from PIL import Image  
import os  
  
base = r'E:\UnityProject\wuziqi\Assets\Art\Cat\Frames'  
TARGET = 512  
count = 0  
for cat in os.listdir(base):  
    catdir = os.path.join(base, cat)  
    if not os.path.isdir(catdir): continue  
    for mood in os.listdir(catdir):  
        mooddir = os.path.join(catdir, mood)  
        if not os.path.isdir(mooddir): continue  
        for fname in os.listdir(mooddir):  
            if not fname.endswith('.png'): continue  
            fpath = os.path.join(mooddir, fname)  
            img = Image.open(fpath)  
            if img.size != (TARGET, TARGET):  
                img = img.resize((TARGET, TARGET), Image.LANCZOS)  
                img.save(fpath)  
                count += 1  
print(f'Resized {count} frames to {TARGET}x{TARGET}') 
