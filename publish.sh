MODEL_PATH="Assets/Vibration"
cat ${MODEL_PATH}/package.json | jq 'del(.version)' | jq '. + { "version": "sdkver" }'>>tmp
mv tmp ${MODEL_PATH}/package.json
sed -i '' 's/sdkver/'"$1"'/'  ${MODEL_PATH}/package.json
git add -A
git commit -m "uv"
git pull
git push
git subtree split --prefix=${MODEL_PATH} --branch upm
git tag $1 upm
git push origin upm --tags
# git push origin --delete upm
# git branch -D upm
# git tag -d $1
# git push origin :refs/tags/$1