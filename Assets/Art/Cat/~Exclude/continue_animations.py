"""
继续生成剩余猫动画
用法: python continue_animations.py
会自动跳过已完成的，从断点继续
"""
import subprocess, sys, os

SCRIPT = r"e:\UnityProject\wuziqi\Assets\Art\Cat\gen_remaining_cats.py"

if __name__ == "__main__":
    print("="*60)
    print("继续生成剩余猫动画")
    print("="*60)

    # 直接运行主脚本，它会自动跳过已完成的
    subprocess.run([sys.executable, SCRIPT], cwd=os.path.dirname(SCRIPT))
