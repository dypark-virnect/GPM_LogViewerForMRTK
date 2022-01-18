cd %~dp0
call npm version patch
git add package.json
git commit -m "version increase before publish"
git push
call npm set registry http://121.162.3.204:4873/
call npm set //121.162.3.204:4873/:_authToken="55atOpxv42ZlXAf4VGzpyA=="
call npm publish
