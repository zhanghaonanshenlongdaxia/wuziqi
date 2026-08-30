import os  
base = r'E:\UnityProject\wuziqi\Assets\Art\Cat\Frames'  
for cat in sorted(os.listdir(base)):  
    catdir = os.path.join(base, cat)  
    if not os.path.isdir(catdir): continue  
    for mood in sorted(os.listdir(catdir)):  
        mooddir = os.path.join(catdir, mood)  
        if not os.path.isdir(mooddir): continue  
        count = len([f for f in os.listdir(mooddir) if f.endswith('.png')])  
        print(f'{cat}/{mood}: {count} frames') 
