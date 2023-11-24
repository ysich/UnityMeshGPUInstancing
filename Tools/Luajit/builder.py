#!/bin/python
# -*- coding: utf-8 -*-
import sys, os, stat
import shutil
import platform

class Master():
	def __init__(self):
		pass

	def Run(self, argv):
		if len(argv) != 4:
			print("args failed")
		else:
			self._pathSrc = argv[1]
			self._pathDst = argv[2]
			self._luaType = argv[3]

			self._pathSrc = self._pathSrc.replace("\\","/")
			self._pathDst = self._pathDst.replace("\\","/")

			if os.path.exists(self._pathDst):
				if os.path.isdir(self._pathDst):
					shutil.rmtree(self._pathDst)

				if os.path.isfile(self._pathDst):
					# os.remove(self._pathDst)
					return

			os.makedirs(self._pathDst)

			self._usejit = True
			self.InitCommand()

			self.BuildPath(self._pathSrc)

	def InitCommand(self):
		self._cmd = ''
		if 'Windows' in platform.system():
			# Windows
			if self._luaType == '1':          
				self._cmd += 'luajit32.exe'
				os.chdir(os.path.normpath('Tools/LuaJit/Luajit'))
			elif self._luaType == '2':        
				self._cmd += 'luajit64.exe'
				os.chdir(os.path.normpath('Tools/LuaJit/Luajit64'))
			else:                            
				self._usejit = False
				self._cmd += 'luac.exe'
				os.chdir(os.path.normpath('Tools/LuaJit/win'))
				
			 
		elif 'Darwin' in platform.system():
			# OSX
			if self._luaType == '1':
				self._cmd += './luajit32'
			elif self._luaType == '2':
				self._cmd += './luajit64'
			else:
				self._usejit = False
				self._cmd += './luac'

			os.chdir(os.path.normpath('Tools/LuaJit/mac'))
			os.chmod(self._cmd, stat.S_IRWXU | stat.S_IRGRP | stat.S_IROTH)

	# 编译文件夹
	def BuildPath(self, path):
		for parent, dirnames, filenames in os.walk(path, topdown=True, onerror=None):
			for filename in filenames:
				if filename.endswith('.lua'):
					self.BuildFile(parent, filename)

	# 编译文件
	def BuildFile(self, path, filename):
		newfilename = '%s_%s' %(path.replace(self._pathSrc, '').replace('/', '_').replace('\\', '_'), filename.replace('.lua', '.lua.bytes'))
		if newfilename.startswith('_'):
			newfilename = newfilename[1:]

		inpath = os.path.normpath('%s/%s' % (path, filename))
		outpath = os.path.normpath('%s/%s' % (self._pathDst, newfilename))
		
		if self._usejit:
			os.system('%s -b %s %s' % (self._cmd, inpath, outpath))
		else:
			shutil.copyfile(inpath, outpath)

if __name__ == '__main__':
	Master().Run(sys.argv)
