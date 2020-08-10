if [ ! $# -eq 2 ]; then
  echo "Usage: ./build_release [RID] [TAG]"
  echo "Example:"
  echo "RID: for example: win-x64, win-x86, linux-x64, osx.10.12-x64"
  echo "TAG: A TAG to include in the release archive filename"
  exit 1
fi

RID=$1
TAG_NAME=$2
PROJ_ROOT=$(pwd)

PUBLISH_ARGS="/p:PublishSingleFile=true"

# Cleanup first
dotnet clean
rm -fR publish-$RID
rm -fr release/XVDTool-$RID-$TAG_NAME.zip

mkdir release

# Build core library
# dotnet publish -c Release -r $RID -f netstandard2.1 -o publish-$RID LibXboxOne

# Publish builds
for proj in {XVDTool,XBFSTool,DurangoKeyExtractor}
do
    dotnet publish $PUBLISH_ARGS -c Release -r $RID -f netcoreapp3.1 -o publish-$RID $proj --self-contained false
done

# Bundle additional files
cp README.md publish-$RID/
cp CHANGELOG.md publish-$RID/

# Package up
cd publish-$RID
zip -r ../release/XVDTool-$RID-$TAG_NAME.zip .
