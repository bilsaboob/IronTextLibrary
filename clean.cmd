@echo off

pushd %~dp0Src

rmdir /s /q IronText.Build\bin
rmdir /s /q IronText.Build\obj
rmdir /s /q Tools\IronText.MsBuild\bin
rmdir /s /q Tools\IronText.MsBuild\obj
rmdir /s /q Tools\IronText.MsBuild.Tests\bin
rmdir /s /q Tools\IronText.MsBuild.Tests\obj

rmdir /s /q IronText.Core\bin
rmdir /s /q IronText.Core\obj
rmdir /s /q IronText.Core.Tests\bin
rmdir /s /q IronText.Core.Tests\obj
rmdir /s /q IronText.Compiler\bin
rmdir /s /q IronText.Compiler\obj

rmdir /s /q Tools\MergeDerived\bin
rmdir /s /q Tools\MergeDerived\obj
rmdir /s /q Tools\ImportBnf\bin
rmdir /s /q Tools\ImportBnf\obj
rmdir /s /q Sandbox\IronText.Lib.Stem\bin
rmdir /s /q Sandbox\IronText.Lib.Stem\obj
rmdir /s /q Sandbox\IronText.Lib.Stem.Tests\bin
rmdir /s /q Sandbox\IronText.Lib.Stem.Tests\obj

popd

rmdir /s /q Samples\Calculator\bin
rmdir /s /q Samples\Calculator\obj
rmdir /s /q Samples\MyArchiver\bin
rmdir /s /q Samples\MyArchiver\obj
rmdir /s /q Samples\DynamicLinq\bin
rmdir /s /q Samples\DynamicLinq\obj
rmdir /s /q Samples\NestedComments\bin
rmdir /s /q Samples\NestedComments\obj
rmdir /s /q Samples\ImportBnf\bin
rmdir /s /q Samples\ImportBnf\obj


del /s /q *.user
del /s /q /aH *.suo
del /s /q TestResult.xml
del /s /q *.VisualState.xml

rmdir /s /q %~dp0deploy\lib
rmdir /s /q %~dp0deploy\tools
rmdir /s /q %~dp0deploy\build
rmdir /s /q %~dp0deploy\content
:: rmdir /s /q %~dp0deploy\content
rmdir /s /q %~dp0Samples\packages\IronText.0.9.0.0
del /s /q %~dp0install
