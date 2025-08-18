@echo off
pushd "%DROPBOX_TEAM%\Documents\Language\Chinese\mandarin blueprint\language islands\audio files"
python "%DROPBOX_TEAM%\Documents\Language\Chinese\mandarin blueprint\scripts\update_mb_li_tags.py"
popd
pause