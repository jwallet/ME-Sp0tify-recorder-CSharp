docker run --rm --label=jekyll --volume=D:\dev\spy-spotify\:/srv/jekyll -it -p 4000:4000 jekyll/jekyll jekyll serve --trace --force_polling -w --baseurl /spy-spotify