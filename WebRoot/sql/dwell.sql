CREATE TABLE IF NOT EXISTS `landDwell` (
  `id` char(36) NOT NULL,
  `pid` char(36) NOT NULL,
  `timestamp` int(20) NOT NULL,
  PRIMARY KEY (`timestamp`),
  UNIQUE KEY `timestamp` (`timestamp`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
