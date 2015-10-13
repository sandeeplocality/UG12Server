<?php

if (!isset($argv[1])) die('Please provide the zone id as a command line argument.');

/*****  MySQL Database Settings  *****/
define('DB_HOST', 			'localhost');
define('DB_USER', 			'franciscopreller');
define('DB_PASSWORD', 		'Xohkoov4');
define('DATABASE', 			'ug12_maindb');

// Get zone id
$zone_id = $argv[1];

// Get gmt offset
$db = new MysqlDB();
        
// Get verbal zone
$zone_result = $db->query("SELECT zone.zone_name FROM zone WHERE id = %s", $zone_id);
$zone        = $db->fetch_array($zone_result);
$zone_name   = $zone[0];

// Adjust timezone
$origin_dtz = new DateTimeZone($zone_name);
$remote_dtz = new DateTimeZone("UTC");
$origin_dt  = new DateTime("now", $remote_dtz);
$remote_dt  = new DateTime("now", $origin_dtz);

// Output gmt offset
echo $remote_dtz->getOffset($remote_dt) + $origin_dtz->getOffset($origin_dt);

/********************************************
 * DATABASE CLASS
 ********************************************/

/**
 * Database class. Why? Because I would rather change the definitions here.
 * @author Francisco Preller
 */

class Database {
    
    private $link;
    private $query;
    private $result;
    
    public function connect($server, $username, $password, $db) {
        $this->link = new mysqli($server, $username, $password, $db);
    }
    
    public function errno() {
        return mysqli_errno($this->link);
    }
    
    public function error() {
        return mysqli_error($this->link);
    }
    
    public function insert_id() {
        return mysqli_insert_id($this->link);
    }
    
    public function escape_string($string) {
        return mysqli_real_escape_string($this->link, $string);
    }
    
    public function escape_args($args) {
        foreach ($args as $i => $arg) {
            if (!is_numeric($arg) && !empty($arg)) {
                $args[$i] = $this->escape_string($arg);
            }
        }
        
        return $args;
    }
    
    public function prepare_args($args) {
        /* escape and add single quotes to strings */
        $args = $this->escape_args($args);
        foreach ($args as $i => $arg) {
            if ($arg==='')
                $args[$i] = 'null';
            else
                $args[$i] = "'" . $arg . "'";
        }
        
        return $args;
    }
    
    public function set_query($query, $args = false) {
        /* if there are args present */
        if ($args) {
            /* prepare arguments */
            $args = $this->prepare_args($args);
            /* replace string with args */
            $query = vsprintf($query, $args);
        }
        /* add the query to a variable */
        $this->query = $query;
    }
    
    public function get_query() {
        return $this->query;
    }
    
    public function query($query, $args = false) {
        /* if args are not an array, turn them into one */
        if (!is_array($args)) {
            $args  = func_get_args();
            /* remove query from args */
            unset($args[0]);
        }
        /* set the query */
        $this->set_query($query, $args);
        $this->result = mysqli_query($this->link, $this->get_query());
        
        if (!$this->result) new DatabaseLog($this->errno(), $this->error(), $this->get_query());
        
        return $this->result;
    }
    
    public function fetch_array($result = null, $array_type = MYSQL_BOTH) {
        return mysqli_fetch_array($result === null ? $this->result : $result, $array_type);
    }
    
    public function fetch_results($result = null) {
        $results = array();
        
        /* loop over fetch array results until we have them all */
        while ($row = $this->fetch_array($result === null ? $this->result : $result)) {
            $results[] = $row;
        }
        
        return $results;
    }
    
    public function fetch_row($result = null) {
        return mysqli_fetch_row($result === null ? $this->result : $result);
    }
    
    public function fetch_assoc($result = null) {
        return mysqli_fetch_assoc($result === null ? $this->result : $result);
    }
    
    public function fetch_object($result = null) {
        return mysqli_fetch_object($result === null ? $this->result : $result);
    }
    
    public function num_rows($result = null) {
        return mysqli_num_rows($result === null ? $this->result : $result);
    }
    
    public function close() {
        return mysqli_close($this->link);
    }
}

class DatabaseLog {
    
    private $errno;
    private $error;
    
    public function __construct($errno, $error, $query) {
        $this->errno = $errno;
        $this->error = $error;
        $this->write_to_log($query);
    }
    
    private function write_to_log($query) {
        $date_now  = new DateTime();
        $date_time = $date_now->format('d-M-Y H:i:s');
        $query     = preg_replace('/\s\s+/', ' ', $query);
        $message   = "[$date_time] MySQL Error ($this->errno): $this->error in query: $query\r\n";
        
        $fh = fopen(SQL_LOG, 'a');
        fwrite($fh, $message);
        fclose($fh);
    }
    
}

class MysqlDB extends Database {
    
    public function __construct($dbname = DATABASE) {
        /* connection credentials */
        $server   = DB_HOST;
        $username = DB_USER;
        $password = DB_PASSWORD;
        
        $this->connect($server, $username, $password, $dbname); // make the connection
          
        if ($this->errno()) {
            throw new Exception("Could not connect to MySQL: \n" . $this->error());
        }
    }
    
    /**
     * Gets a set of arguments and compares them against the table, returning only
     * valid keys and values which can be inserted or updated in that table.
     * @param string $table
     * @param array $values
     * @return array 
     */
    private function filter_args($table, $values) {
        $columns = array();
        $args    = array();
        $cols    = array();
        
        /* get table columns */
        $q1     = $this->query("SHOW COLUMNS FROM $table");
        while($row = $this->fetch_array($q1)) {
            $cols[] = $row['Field'];
        }
        /* separate keys from values (only for existing fields) */
        foreach ($values as $key => $val) {
            if (in_array($key, $cols) && !is_numeric($key)) {
                $columns[] = $key;
                $args[]    = $val;
            } else {
                /* otherwise unset the argument */
                unset($values[$key]);
            }
        }
        /* escape and clean args */
        $columns   = $this->escape_args($columns);
        $args      = $this->prepare_args($values);
        
        return array('cols' => $columns, 'args' => $args);
    }
    
    /**
     * Inserts a new row into a table
     * @param string $table
     * @param array $arg_array
     * @return mysqli resource 
     */
    public function insert($table, $arg_array) {
        $values = $this->filter_args($table, $arg_array);
        
        /* stringify columns and args */
        $columnstr = implode(', ', $values['cols']);
        $valuestr  = implode(', ', $values['args']);
        
        return $this->query("INSERT INTO $table ($columnstr) VALUES ($valuestr)");
    }
    
    public function update($table, $arg_array, $id) {
        $init   = $this->filter_args($table, $arg_array);
        $values = array_combine($init['cols'], $init['args']);
        
        /* build query */
        $query    = "UPDATE $table SET ";
        $keys     = array_keys($values);
        $last_key = end($keys);
        foreach ($values as $col => $val) {
            if ($val===null) $val = 'null';
            $query .= $col . " = " . $val;
            if ($col!=$last_key) $query .= ", ";
        }
        $query .= " WHERE id = %s";

        return $this->query($query, $id);
    }
    
    public function delete($table, $id) {
        return $this->query("DELETE FROM $table WHERE id = %s", $id);
    }
    
}

?>